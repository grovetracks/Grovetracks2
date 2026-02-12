using System.Net.Http.Json;
using System.Text.Json;
using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Generation;
using Grovetracks.Etl.Data;
using Grovetracks.Etl.Mappers;
using Grovetracks.Etl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Grovetracks.Etl.Operations;

public static class GenerateLocalAiCompositionsOperation
{
    private const int DefaultPerSubject = 5;
    private const double DefaultMinQualityThreshold = 0.20;
    private const string SourceTypeValue = "ai-generated";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task RunAsync(
        IConfiguration config,
        string modelName,
        int perSubject = DefaultPerSubject,
        bool truncate = false,
        bool dryRun = false,
        bool resume = false)
    {
        var connectionString = config["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured");

        var ollamaUrl = config["Ollama:Url"] ?? "http://localhost:11434";
        var maxRetries = int.TryParse(config["Ollama:MaxRetries"], out var r) ? r : 3;
        var timeoutMinutes = int.TryParse(config["Ollama:TimeoutMinutes"], out var t) ? t : 10;
        var generationMethod = $"ollama-{modelName}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        if (truncate && !dryRun)
        {
            Console.WriteLine("Removing previously AI-generated compositions...");
            await using var truncateDb = new AppDbContext(options);
            var deleted = await truncateDb.SeedCompositions
                .Where(s => s.SourceType == SourceTypeValue)
                .ExecuteDeleteAsync();
            Console.WriteLine($"Removed {deleted:N0} AI-generated compositions.");
        }

        var subjects = ComposableSubjects.All;
        var subjectsToProcess = subjects.ToList();

        if (resume && !dryRun)
        {
            Console.WriteLine("Checking for existing AI-generated compositions...");
            await using var checkDb = new AppDbContext(options);
            var existingCounts = await checkDb.SeedCompositions
                .Where(s => s.SourceType == SourceTypeValue && s.GenerationMethod == generationMethod)
                .GroupBy(s => s.Word)
                .Select(g => new { Word = g.Key, Count = g.Count() })
                .ToListAsync();

            var completedSubjects = existingCounts
                .Where(x => x.Count >= perSubject)
                .Select(x => x.Word)
                .ToHashSet();

            subjectsToProcess = subjectsToProcess
                .Where(s => !completedSubjects.Contains(s))
                .ToList();

            Console.WriteLine($"Skipping {subjects.Count - subjectsToProcess.Count} already-completed subjects.");
        }

        Console.WriteLine($"Ollama URL: {ollamaUrl}");
        Console.WriteLine($"Model: {modelName}");
        Console.WriteLine($"Subjects: {subjectsToProcess.Count} to process (of {subjects.Count} total)");
        Console.WriteLine($"Per subject: {perSubject} compositions");
        Console.WriteLine($"Mode: {(dryRun ? "DRY RUN" : "LIVE")}");
        Console.WriteLine();

        if (dryRun)
        {
            Console.WriteLine("=== DRY RUN — No API calls or database writes ===");
            Console.WriteLine($"Would generate up to {subjectsToProcess.Count * perSubject:N0} compositions");
            Console.WriteLine($"Estimated API calls: {subjectsToProcess.Count}");
            Console.WriteLine();

            Console.Write("Checking Ollama connectivity... ");
            using var checkClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            try
            {
                var tagsResponse = await checkClient.GetAsync($"{ollamaUrl}/api/tags");
                tagsResponse.EnsureSuccessStatusCode();
                var tagsJson = await tagsResponse.Content.ReadAsStringAsync();
                var modelExists = tagsJson.Contains(modelName, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(modelExists
                    ? $"OK (model '{modelName}' found)"
                    : $"OK (connected, but model '{modelName}' not found — run setup-model.sh first)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED ({ex.Message})");
            }

            Console.WriteLine();
            Console.WriteLine("Sample subjects:");
            foreach (var subject in subjectsToProcess.Take(20))
                Console.WriteLine($"  - {subject}");
            if (subjectsToProcess.Count > 20)
                Console.WriteLine($"  ... and {subjectsToProcess.Count - 20} more");
            return;
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(timeoutMinutes) };

        Console.Write("Verifying Ollama is reachable... ");
        try
        {
            var tagsResponse = await httpClient.GetAsync($"{ollamaUrl}/api/tags");
            tagsResponse.EnsureSuccessStatusCode();
            var tagsJson = await tagsResponse.Content.ReadAsStringAsync();
            if (!tagsJson.Contains(modelName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"WARNING: model '{modelName}' not found. Pull it first:");
                Console.WriteLine($"  curl -X POST {ollamaUrl}/api/pull -d '{{\"name\":\"{modelName}\"}}'");
                return;
            }
            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED — cannot reach Ollama at {ollamaUrl}: {ex.Message}");
            return;
        }

        using var cts = new CancellationTokenSource();
        var cancelled = false;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancelled = true;
            cts.Cancel();
            Console.WriteLine();
            Console.WriteLine("Shutting down gracefully, saving pending compositions...");
        };

        var totalGenerated = 0;
        var totalValid = 0;
        var failedSubjects = new List<string>();

        for (var i = 0; i < subjectsToProcess.Count; i++)
        {
            if (cancelled)
                break;

            var subject = subjectsToProcess[i];
            var progress = $"[{i + 1}/{subjectsToProcess.Count}]";

            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (cancelled)
                    break;

                try
                {
                    var userPrompt = AiCompositionPrompts.BuildUserPrompt(subject, perSubject);
                    var temperature = 0.7 + (attempt * 0.1);

                    var requestBody = new
                    {
                        model = modelName,
                        messages = new object[]
                        {
                            new { role = "system", content = AiCompositionPrompts.SystemPrompt },
                            new { role = "user", content = userPrompt }
                        },
                        stream = false,
                        format = AiCompositionPrompts.CompositionSchema,
                        options = new { num_predict = 4096, temperature }
                    };

                    var response = await httpClient.PostAsJsonAsync(
                        $"{ollamaUrl}/api/chat", requestBody, cts.Token);
                    response.EnsureSuccessStatusCode();

                    var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                        JsonOptions, cts.Token);

                    if (ollamaResponse?.Message.Content is null)
                    {
                        if (attempt == maxRetries)
                        {
                            Console.WriteLine($"  {progress} {subject}: no content in response after {maxRetries + 1} attempts");
                            failedSubjects.Add(subject);
                        }
                        continue;
                    }

                    var batch = JsonSerializer.Deserialize<AiCompositionBatch>(
                        ollamaResponse.Message.Content, JsonOptions);

                    if (batch?.Compositions is null || batch.Compositions.Count == 0)
                    {
                        if (attempt == maxRetries)
                        {
                            Console.WriteLine($"  {progress} {subject}: empty or invalid response after {maxRetries + 1} attempts");
                            failedSubjects.Add(subject);
                        }
                        continue;
                    }

                    var subjectGenerated = 0;
                    var subjectValid = 0;
                    var subjectBatch = new List<SeedComposition>();

                    foreach (var aiComp in batch.Compositions)
                    {
                        subjectGenerated++;
                        totalGenerated++;

                        var composition = AiCompositionMapper.MapToComposition(aiComp, generationMethod);
                        var (isValid, qualityScore) = CompositionValidator.Validate(composition);

                        if (!isValid || qualityScore < DefaultMinQualityThreshold)
                            continue;

                        var compositionJson = JsonSerializer.Serialize(composition, JsonOptions);

                        subjectBatch.Add(new SeedComposition
                        {
                            Id = Guid.NewGuid(),
                            Word = subject,
                            SourceKeyId = "ai-generated",
                            QualityScore = qualityScore,
                            StrokeCount = CompositionGeometry.CountTotalStrokes(composition),
                            TotalPointCount = CompositionGeometry.CountTotalPoints(composition),
                            CompositionJson = compositionJson,
                            CuratedAt = DateTime.UtcNow,
                            SourceType = SourceTypeValue,
                            GenerationMethod = generationMethod,
                            SourceCompositionIds = null
                        });

                        subjectValid++;
                        totalValid++;
                    }

                    if (subjectBatch.Count > 0)
                        await FlushBatchAsync(options, subjectBatch);

                    Console.WriteLine($"  {progress} {subject}: {subjectGenerated} generated, {subjectValid} valid (saved)");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"  {progress} {subject}: cancelled");
                    goto loopEnd;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"  {progress} {subject}: attempt {attempt + 1} failed — {ex.Message}, retrying...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {progress} {subject}: ERROR — {ex.Message}");
                    failedSubjects.Add(subject);
                }
            }
        }

        loopEnd:
        Console.WriteLine();
        if (cancelled)
        {
            Console.WriteLine("=== Local AI Generation Summary (Cancelled by user) ===");
            Console.WriteLine($"  Model:      {modelName}");
            Console.WriteLine($"  Generated:  {totalGenerated:N0}");
            Console.WriteLine($"  Saved:      {totalValid:N0}");
            Console.WriteLine("  All completed subjects were saved to the database.");
            Console.WriteLine("  Re-run with --resume to continue where you left off.");
        }
        else
        {
            Console.WriteLine("=== Local AI Generation Summary ===");
            Console.WriteLine($"  Model:      {modelName}");
            Console.WriteLine($"  Generated:  {totalGenerated:N0}");
            Console.WriteLine($"  Valid:      {totalValid:N0}");
            Console.WriteLine($"  Failed:     {failedSubjects.Count} subjects");
            if (failedSubjects.Count > 0)
            {
                Console.WriteLine($"  Failed subjects: {string.Join(", ", failedSubjects)}");
                Console.WriteLine("  (Re-run with --resume to retry failed subjects)");
            }
        }
    }

    private static async Task FlushBatchAsync(
        DbContextOptions<AppDbContext> options,
        List<SeedComposition> batch)
    {
        await using var db = new AppDbContext(options);
        db.SeedCompositions.AddRange(batch);
        await db.SaveChangesAsync();
    }
}
