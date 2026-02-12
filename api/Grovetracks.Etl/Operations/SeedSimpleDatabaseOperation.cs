using System.Text.Json;
using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Entities;
using Grovetracks.Etl.Models;
using Grovetracks.Etl.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Grovetracks.Etl.Operations;

public static class SeedSimpleDatabaseOperation
{
    private const int BatchSize = 50000;

    public static async Task RunAsync(IConfiguration config, bool truncate = false)
    {
        var datasetPath = config["Etl:SimpleDatasetPath"]
            ?? throw new InvalidOperationException("Etl:SimpleDatasetPath not configured");
        var connectionString = config["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured");
        var seedProgressFilePath = config["Etl:SimpleSeedProgressFilePath"] ?? "progress-seed-simple.txt";

        Console.WriteLine($"Dataset path: {datasetPath}");
        Console.WriteLine($"Seed progress file: {Path.GetFullPath(seedProgressFilePath)}");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        if (truncate)
        {
            Console.WriteLine("Truncating quickdraw_simple_doodles table...");
            await using var truncateDb = new AppDbContext(options);
            await truncateDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE quickdraw_simple_doodles");
            Console.WriteLine("Table truncated.");

            File.WriteAllText(seedProgressFilePath, string.Empty);
            Console.WriteLine("Seed progress file cleared.");
        }

        var processedKeys = new HashSet<string>();
        if (File.Exists(seedProgressFilePath))
        {
            foreach (var line in File.ReadLines(seedProgressFilePath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    processedKeys.Add(line.Trim());
            }
            Console.WriteLine($"Loaded {processedKeys.Count:N0} previously seeded key_ids");
        }

        var files = Directory.GetFiles(datasetPath, "*.ndjson");
        Console.WriteLine($"Found {files.Length} NDJSON files");

        using var progressWriter = new StreamWriter(seedProgressFilePath, append: true) { AutoFlush = true };

        var totalInserted = 0;
        var totalSkipped = 0;
        var totalErrors = 0;

        foreach (var file in files.OrderBy(f => f))
        {
            var fileName = Path.GetFileName(file);
            var fileInserted = 0;
            var fileSkipped = 0;
            var fileErrors = 0;

            Console.WriteLine($"\nProcessing: {fileName}");

            var batch = new List<QuickdrawSimpleDoodle>(BatchSize);
            var batchKeys = new List<string>(BatchSize);

            foreach (var line in File.ReadLines(file))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var record = JsonSerializer.Deserialize<QuickdrawRecord>(line);
                    if (record is null || string.IsNullOrEmpty(record.key_id))
                    {
                        fileErrors++;
                        continue;
                    }

                    if (processedKeys.Contains(record.key_id))
                    {
                        fileSkipped++;
                        continue;
                    }

                    var timestamp = TimestampParser.Parse(record.timestamp);

                    var entity = new QuickdrawSimpleDoodle
                    {
                        KeyId = record.key_id,
                        Word = record.word,
                        CountryCode = record.countrycode,
                        Timestamp = timestamp,
                        Recognized = record.recognized,
                        Drawing = record.drawing.GetRawText()
                    };

                    batch.Add(entity);
                    batchKeys.Add(record.key_id);

                    if (batch.Count >= BatchSize)
                    {
                        var inserted = await FlushBatchAsync(options, batch, batchKeys, processedKeys, progressWriter);
                        fileInserted += inserted;
                        fileErrors += batch.Count - inserted;
                        batch.Clear();
                        batchKeys.Clear();

                        if (fileInserted % 10000 == 0)
                        {
                            Console.WriteLine($"  {fileName}: {fileInserted:N0} inserted...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    fileErrors++;
                    if (fileErrors <= 5)
                    {
                        Console.Error.WriteLine($"  Parse error in {fileName}: {ex.Message}");
                    }
                }
            }

            if (batch.Count > 0)
            {
                var inserted = await FlushBatchAsync(options, batch, batchKeys, processedKeys, progressWriter);
                fileInserted += inserted;
            }

            totalInserted += fileInserted;
            totalSkipped += fileSkipped;
            totalErrors += fileErrors;
            Console.WriteLine($"  {fileName}: done — {fileInserted:N0} inserted, {fileSkipped:N0} skipped, {fileErrors} errors");
        }

        Console.WriteLine($"\nComplete: {totalInserted:N0} inserted, {totalSkipped:N0} skipped, {totalErrors} errors across {files.Length} files");
    }

    private static async Task<int> FlushBatchAsync(
        DbContextOptions<AppDbContext> options,
        List<QuickdrawSimpleDoodle> batch,
        List<string> batchKeys,
        HashSet<string> processedKeys,
        StreamWriter progressWriter)
    {
        try
        {
            await using var db = new AppDbContext(options);
            db.QuickdrawSimpleDoodles.AddRange(batch);
            await db.SaveChangesAsync();

            foreach (var key in batchKeys)
            {
                processedKeys.Add(key);
                progressWriter.WriteLine(key);
            }

            return batch.Count;
        }
        catch (DbUpdateException ex)
        {
            Console.Error.WriteLine($"  Batch insert failed ({ex.InnerException?.Message ?? ex.Message}), retrying individually...");
            var inserted = 0;

            for (var i = 0; i < batch.Count; i++)
            {
                try
                {
                    await using var db = new AppDbContext(options);
                    db.QuickdrawSimpleDoodles.Add(batch[i]);
                    await db.SaveChangesAsync();

                    processedKeys.Add(batchKeys[i]);
                    progressWriter.WriteLine(batchKeys[i]);
                    inserted++;
                }
                catch
                {
                    // Skip duplicates or other failures silently
                }
            }

            return inserted;
        }
    }
}
