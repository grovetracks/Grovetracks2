using System.Text.Json;
using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Grovetracks.Etl.Operations;

public static class CurateSimpleDoodlesOperation
{
    private const int BatchSize = 1000;
    private const int DefaultPerCategory = 50;

    private const int MinStrokes = 2;
    private const int MaxStrokes = 20;
    private const int MinPointsPerStroke = 3;
    private const int MaxPointsPerStroke = 200;
    private const int MinTotalPoints = 10;
    private const int MaxTotalPoints = 1000;
    private const double MinBoundingBoxCoverage = 0.15;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task RunAsync(IConfiguration config, int perCategory = DefaultPerCategory, bool truncate = false)
    {
        var connectionString = config["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var mapper = new SimpleCompositionMapper();

        if (truncate)
        {
            Console.WriteLine("Truncating seed_compositions table...");
            await using var truncateDb = new AppDbContext(options);
            await truncateDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE seed_compositions");
            Console.WriteLine("Table truncated.");
        }

        await using var readDb = new AppDbContext(options);

        var words = await readDb.QuickdrawSimpleDoodles
            .Select(d => d.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync();

        Console.WriteLine($"Found {words.Count} categories to curate");
        Console.WriteLine($"Selecting top {perCategory} per category");
        Console.WriteLine($"Heuristics: strokes [{MinStrokes}-{MaxStrokes}], points/stroke [{MinPointsPerStroke}-{MaxPointsPerStroke}], total points [{MinTotalPoints}-{MaxTotalPoints}], bbox coverage >= {MinBoundingBoxCoverage:P0}");
        Console.WriteLine();

        var totalCurated = 0;
        var totalScanned = 0;
        var totalRejected = 0;

        foreach (var word in words)
        {
            await using var wordDb = new AppDbContext(options);

            var existingCount = await wordDb.SeedCompositions
                .Where(s => s.Word == word)
                .CountAsync();

            if (existingCount >= perCategory)
            {
                Console.WriteLine($"  {word}: already has {existingCount} curated — skipping");
                continue;
            }

            var needed = perCategory - existingCount;

            var existingKeyIds = await wordDb.SeedCompositions
                .Where(s => s.Word == word)
                .Select(s => s.SourceKeyId)
                .ToListAsync();

            var existingKeyIdSet = existingKeyIds.ToHashSet();

            var candidates = await wordDb.QuickdrawSimpleDoodles
                .Where(d => d.Word == word && d.Recognized)
                .AsNoTracking()
                .ToListAsync();

            totalScanned += candidates.Count;

            var scored = new List<(QuickdrawSimpleDoodle Doodle, double Score, int Strokes, int Points)>();

            foreach (var candidate in candidates)
            {
                if (existingKeyIdSet.Contains(candidate.KeyId))
                    continue;

                var result = ScoreDoodle(candidate);
                if (result is null)
                {
                    totalRejected++;
                    continue;
                }

                scored.Add((candidate, result.Value.Score, result.Value.StrokeCount, result.Value.TotalPoints));
            }

            var winners = scored
                .OrderByDescending(s => s.Score)
                .Take(needed)
                .ToList();

            if (winners.Count == 0)
            {
                Console.WriteLine($"  {word}: {candidates.Count} scanned, 0 passed filters");
                continue;
            }

            var seedEntities = winners
                .Select(w =>
                {
                    var composition = mapper.MapToComposition(w.Doodle);
                    var compositionJson = JsonSerializer.Serialize(composition, JsonOptions);

                    return new SeedComposition
                    {
                        Id = Guid.NewGuid(),
                        Word = w.Doodle.Word,
                        SourceKeyId = w.Doodle.KeyId,
                        QualityScore = w.Score,
                        StrokeCount = w.Strokes,
                        TotalPointCount = w.Points,
                        CompositionJson = compositionJson,
                        CuratedAt = DateTime.UtcNow
                    };
                })
                .ToList();

            await using var writeDb = new AppDbContext(options);
            writeDb.SeedCompositions.AddRange(seedEntities);
            await writeDb.SaveChangesAsync();

            totalCurated += seedEntities.Count;
            Console.WriteLine($"  {word}: {candidates.Count} scanned, {scored.Count} passed filters, {seedEntities.Count} curated (top score: {winners[0].Score:F2})");
        }

        Console.WriteLine();
        Console.WriteLine($"Complete: {totalCurated:N0} curated across {words.Count} categories ({totalScanned:N0} scanned, {totalRejected:N0} rejected)");
    }

    private static (double Score, int StrokeCount, int TotalPoints)? ScoreDoodle(QuickdrawSimpleDoodle doodle)
    {
        List<List<List<int>>>? strokes;
        try
        {
            strokes = JsonSerializer.Deserialize<List<List<List<int>>>>(doodle.Drawing);
        }
        catch
        {
            return null;
        }

        if (strokes is null || strokes.Count == 0)
            return null;

        var strokeCount = strokes.Count;
        if (strokeCount < MinStrokes || strokeCount > MaxStrokes)
            return null;

        var totalPoints = 0;
        var allXs = new List<int>();
        var allYs = new List<int>();

        foreach (var stroke in strokes)
        {
            if (stroke.Count < 2 || stroke[0].Count == 0)
                return null;

            var pointsInStroke = stroke[0].Count;
            if (pointsInStroke < MinPointsPerStroke || pointsInStroke > MaxPointsPerStroke)
                return null;

            totalPoints += pointsInStroke;
            allXs.AddRange(stroke[0]);
            allYs.AddRange(stroke[1]);
        }

        if (totalPoints < MinTotalPoints || totalPoints > MaxTotalPoints)
            return null;

        var minX = allXs.Min();
        var maxX = allXs.Max();
        var minY = allYs.Min();
        var maxY = allYs.Max();

        var bboxWidth = maxX - minX;
        var bboxHeight = maxY - minY;
        var bboxCoverage = (bboxWidth / 255.0) * (bboxHeight / 255.0);

        if (bboxCoverage < MinBoundingBoxCoverage)
            return null;

        // Score: weighted combination of desirable properties
        // Higher is better
        var strokeScore = 1.0 - Math.Abs(strokeCount - 7.0) / 15.0; // ideal ~7 strokes
        var pointScore = 1.0 - Math.Abs(totalPoints - 80.0) / 500.0; // ideal ~80 points
        var coverageScore = Math.Min(bboxCoverage / 0.6, 1.0); // reward good canvas coverage
        var balanceScore = 1.0 - Math.Abs(bboxWidth - bboxHeight) / 255.0; // reward square-ish drawings

        var score = (strokeScore * 0.2) + (pointScore * 0.2) + (coverageScore * 0.35) + (balanceScore * 0.25);

        return (Math.Round(score, 4), strokeCount, totalPoints);
    }
}
