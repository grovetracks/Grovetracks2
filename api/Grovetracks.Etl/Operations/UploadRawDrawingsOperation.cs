using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Grovetracks.Etl.Models;
using Microsoft.Extensions.Configuration;

namespace Grovetracks.Etl.Operations;

public static class UploadRawDrawingsOperation
{
    public static async Task RunAsync(IConfiguration config)
    {
        var datasetPath = config["Etl:DatasetPath"] ?? throw new InvalidOperationException("Etl:DatasetPath not configured");
        var bucketName = config["Etl:BucketName"] ?? throw new InvalidOperationException("Etl:BucketName not configured");
        var maxConcurrency = int.Parse(config["Etl:MaxConcurrency"] ?? "10");
        var progressFilePath = config["Etl:ProgressFilePath"] ?? "progress-upload.txt";

        Console.WriteLine($"Dataset path: {datasetPath}");
        Console.WriteLine($"Bucket: {bucketName}");
        Console.WriteLine($"Max concurrency: {maxConcurrency}");
        Console.WriteLine($"Progress file: {Path.GetFullPath(progressFilePath)}");

        var processedKeys = new HashSet<string>();
        if (File.Exists(progressFilePath))
        {
            foreach (var line in File.ReadLines(progressFilePath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    processedKeys.Add(line.Trim());
            }
            Console.WriteLine($"Loaded {processedKeys.Count} previously processed key_ids");
        }

        var files = Directory.GetFiles(datasetPath, "*.ndjson");
        Console.WriteLine($"Found {files.Length} NDJSON files");

        using var s3Client = new AmazonS3Client();
        using var progressWriter = new StreamWriter(progressFilePath, append: true) { AutoFlush = true };
        var progressLock = new object();

        var totalUploaded = 0;
        var totalSkipped = 0;
        var totalErrors = 0;

        foreach (var file in files.OrderBy(f => f))
        {
            var fileName = Path.GetFileName(file);
            var fileUploaded = 0;
            var fileSkipped = 0;
            var fileErrors = 0;

            Console.WriteLine($"\nProcessing: {fileName}");

            var lines = File.ReadLines(file)
                .Where(line => !string.IsNullOrWhiteSpace(line));

            await Parallel.ForEachAsync(
                lines,
                new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
                async (line, cancellationToken) =>
                {
                    try
                    {
                        var record = JsonSerializer.Deserialize<QuickdrawRecord>(line);
                        if (record is null || string.IsNullOrEmpty(record.key_id))
                        {
                            Interlocked.Increment(ref fileErrors);
                            return;
                        }

                        if (processedKeys.Contains(record.key_id))
                        {
                            Interlocked.Increment(ref fileSkipped);
                            return;
                        }

                        var drawingJson = record.drawing.GetRawText();

                        var request = new PutObjectRequest
                        {
                            BucketName = bucketName,
                            Key = record.key_id,
                            ContentBody = drawingJson,
                            ContentType = "application/json"
                        };

                        await s3Client.PutObjectAsync(request, cancellationToken);

                        lock (progressLock)
                        {
                            processedKeys.Add(record.key_id);
                            progressWriter.WriteLine(record.key_id);
                        }

                        var count = Interlocked.Increment(ref fileUploaded);
                        if (count % 1000 == 0)
                        {
                            Console.WriteLine($"  {fileName}: {count} uploaded...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref fileErrors);
                        if (fileErrors <= 5)
                        {
                            Console.Error.WriteLine($"  Error in {fileName}: {ex.Message}");
                        }
                    }
                });

            totalUploaded += fileUploaded;
            totalSkipped += fileSkipped;
            totalErrors += fileErrors;
            Console.WriteLine($"  {fileName}: done — {fileUploaded} uploaded, {fileSkipped} skipped, {fileErrors} errors");
        }

        Console.WriteLine($"\nComplete: {totalUploaded} uploaded, {totalSkipped} skipped, {totalErrors} errors across {files.Length} files");
    }
}
