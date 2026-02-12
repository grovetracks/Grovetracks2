using System.Text.Json;
using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Grovetracks.Etl.Operations;

public static class GenerateCompositionsOperation
{
    private const int BatchSize = 500;
    private const int DefaultPerCategory = 5;
    private const int DefaultScenes = 100;
    private const double MinQualityThreshold = 0.30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task RunAsync(
        IConfiguration config,
        int perCategory = DefaultPerCategory,
        int scenes = DefaultScenes,
        int? seed = null,
        bool truncateGenerated = false,
        bool dryRun = false)
    {
        var connectionString = config["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        if (truncateGenerated && !dryRun)
        {
            Console.WriteLine("Removing previously generated compositions...");
            await using var truncateDb = new AppDbContext(options);
            var deleted = await truncateDb.SeedCompositions
                .Where(s => s.SourceType == "generated")
                .ExecuteDeleteAsync();
            Console.WriteLine($"Removed {deleted:N0} generated compositions.");
        }

        await using var readDb = new AppDbContext(options);

        var words = await readDb.SeedCompositions
            .Where(s => s.SourceType == "curated")
            .Select(s => s.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync();

        Console.WriteLine($"Found {words.Count} curated categories");
        Console.WriteLine($"Mode: {(dryRun ? "DRY RUN" : "LIVE")}");
        Console.WriteLine($"Augmentation: {perCategory} variations per curated seed");
        Console.WriteLine($"Scenes: {scenes} multi-fragment compositions");
        Console.WriteLine();

        // Phase A: Load all curated seeds indexed by word
        var curatedByWord = new Dictionary<string, List<(SeedComposition Entity, Composition Comp)>>();
        foreach (var word in words)
        {
            await using var wordDb = new AppDbContext(options);
            var seeds = await wordDb.SeedCompositions
                .Where(s => s.Word == word && s.SourceType == "curated")
                .OrderByDescending(s => s.QualityScore)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            var parsed = seeds
                .Select(s =>
                {
                    var comp = JsonSerializer.Deserialize<Composition>(s.CompositionJson, JsonOptions);
                    return comp is not null ? (s, comp) : default;
                })
                .Where(x => x.comp is not null)
                .ToList();

            if (parsed.Count > 0)
                curatedByWord[word] = parsed!;
        }

        Console.WriteLine($"Loaded {curatedByWord.Values.Sum(v => v.Count)} curated seeds across {curatedByWord.Count} categories");
        Console.WriteLine();

        // Phase B: Generate augmented variations
        var pipeline = new AugmentationPipeline();
        var augmentedTotal = 0;
        var augmentedBatch = new List<SeedComposition>();

        foreach (var (word, curatedList) in curatedByWord)
        {
            var wordAugmented = 0;

            foreach (var (entity, comp) in curatedList)
            {
                var variations = pipeline.GenerateVariations(comp, perCategory, rng);

                foreach (var (varComp, method) in variations)
                {
                    var (isValid, qualityScore) = CompositionValidator.Validate(varComp);
                    if (!isValid || qualityScore < MinQualityThreshold)
                        continue;

                    var augmentedComp = new Composition
                    {
                        Width = varComp.Width,
                        Height = varComp.Height,
                        DoodleFragments = varComp.DoodleFragments,
                        Tags = new List<string> { "generated", "augmented", word }.AsReadOnly()
                    };

                    var compositionJson = JsonSerializer.Serialize(augmentedComp, JsonOptions);

                    augmentedBatch.Add(new SeedComposition
                    {
                        Id = Guid.NewGuid(),
                        Word = word,
                        SourceKeyId = "generated",
                        QualityScore = qualityScore,
                        StrokeCount = CompositionGeometry.CountTotalStrokes(augmentedComp),
                        TotalPointCount = CompositionGeometry.CountTotalPoints(augmentedComp),
                        CompositionJson = compositionJson,
                        CuratedAt = DateTime.UtcNow,
                        SourceType = "generated",
                        GenerationMethod = $"augmented-{method}",
                        SourceCompositionIds = JsonSerializer.Serialize(new[] { entity.Id })
                    });

                    wordAugmented++;
                }

                if (augmentedBatch.Count >= BatchSize && !dryRun)
                {
                    await FlushBatchAsync(options, augmentedBatch);
                    augmentedBatch.Clear();
                }
            }

            augmentedTotal += wordAugmented;
            if (wordAugmented > 0)
                Console.WriteLine($"  {word}: {wordAugmented} augmented variations");
        }

        if (augmentedBatch.Count > 0 && !dryRun)
        {
            await FlushBatchAsync(options, augmentedBatch);
            augmentedBatch.Clear();
        }

        Console.WriteLine($"\nAugmentation complete: {augmentedTotal:N0} generated");

        // Phase C: Generate multi-fragment scenes
        var sceneComposer = new SceneComposer();
        var allWords = curatedByWord.Keys.ToList();
        var sceneTotal = 0;
        var sceneBatch = new List<SeedComposition>();

        if (allWords.Count >= 2)
        {
            Console.WriteLine($"\nGenerating {scenes} scene compositions...");

            for (var i = 0; i < scenes; i++)
            {
                var template = SceneTemplateProvider.GetRandom(rng);
                var slotAssignments = new List<(Composition Source, string Word)>();
                var usedWords = new List<string>();

                foreach (var slot in template.Slots)
                {
                    var word = allWords[rng.Next(allWords.Count)];
                    var candidates = curatedByWord[word];
                    var (_, comp) = candidates[rng.Next(candidates.Count)];
                    slotAssignments.Add((comp, word));
                    usedWords.Add(word);
                }

                var scene = sceneComposer.ComposeScene(template, slotAssignments);
                var (isValid, qualityScore) = CompositionValidator.Validate(scene);

                if (!isValid || qualityScore < MinQualityThreshold)
                    continue;

                var compositionJson = JsonSerializer.Serialize(scene, JsonOptions);
                var primaryWord = usedWords[0];

                var sourceIds = slotAssignments
                    .Select((_, idx) =>
                    {
                        var w = usedWords[idx];
                        return curatedByWord[w][0].Entity.Id;
                    })
                    .ToArray();

                sceneBatch.Add(new SeedComposition
                {
                    Id = Guid.NewGuid(),
                    Word = primaryWord,
                    SourceKeyId = "generated",
                    QualityScore = qualityScore,
                    StrokeCount = CompositionGeometry.CountTotalStrokes(scene),
                    TotalPointCount = CompositionGeometry.CountTotalPoints(scene),
                    CompositionJson = compositionJson,
                    CuratedAt = DateTime.UtcNow,
                    SourceType = "generated",
                    GenerationMethod = $"scene-{template.Name}",
                    SourceCompositionIds = JsonSerializer.Serialize(sourceIds)
                });

                sceneTotal++;

                if (sceneBatch.Count >= BatchSize && !dryRun)
                {
                    await FlushBatchAsync(options, sceneBatch);
                    sceneBatch.Clear();
                }
            }

            if (sceneBatch.Count > 0 && !dryRun)
            {
                await FlushBatchAsync(options, sceneBatch);
                sceneBatch.Clear();
            }

            Console.WriteLine($"Scene generation complete: {sceneTotal:N0} generated");
        }
        else
        {
            Console.WriteLine("\nSkipping scene generation: need at least 2 categories with curated seeds");
        }

        // Phase D: Report
        Console.WriteLine();
        Console.WriteLine("=== Generation Summary ===");
        Console.WriteLine($"  Augmented: {augmentedTotal:N0}");
        Console.WriteLine($"  Scenes:    {sceneTotal:N0}");
        Console.WriteLine($"  Total:     {augmentedTotal + sceneTotal:N0}");
        if (dryRun)
            Console.WriteLine("  (DRY RUN — nothing was written to database)");
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
