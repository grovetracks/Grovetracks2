using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Grovetracks.Etl.Operations;

public static class DownloadSimpleDrawingsOperation
{
    public static async Task RunAsync(IConfiguration config)
    {
        var bucketName = config["Etl:SimpleBucketName"] ?? throw new InvalidOperationException("Etl:SimpleBucketName not configured");
        var outputPath = config["Etl:SimpleDatasetPath"] ?? throw new InvalidOperationException("Etl:SimpleDatasetPath not configured");
        var maxConcurrency = int.Parse(config["Etl:MaxConcurrency"] ?? "10");

        Console.WriteLine($"Bucket: {bucketName}");
        Console.WriteLine($"Output path: {outputPath}");
        Console.WriteLine($"Max concurrency: {maxConcurrency}");

        Directory.CreateDirectory(outputPath);

        using var s3Client = new AmazonS3Client();

        Console.WriteLine("Listing objects in bucket...");
        var objects = await ListAllObjectsAsync(s3Client, bucketName);
        Console.WriteLine($"Found {objects.Count} objects ({FormatBytes(objects.Sum(o => o.Size ?? 0))} total)");

        var toDownload = objects
            .Where(obj =>
            {
                var localPath = Path.Combine(outputPath, obj.Key);
                if (!File.Exists(localPath)) return true;

                var localSize = new FileInfo(localPath).Length;
                return localSize != (obj.Size ?? 0);
            })
            .ToList();

        var skipped = objects.Count - toDownload.Count;
        if (skipped > 0)
            Console.WriteLine($"Skipping {skipped} files already downloaded with matching size");

        if (toDownload.Count == 0)
        {
            Console.WriteLine("All files already downloaded.");
            return;
        }

        Console.WriteLine($"Downloading {toDownload.Count} files...");

        var downloaded = 0;
        var errors = 0;
        var totalBytes = 0L;

        await Parallel.ForEachAsync(
            toDownload,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
            async (obj, cancellationToken) =>
            {
                var localPath = Path.Combine(outputPath, obj.Key);

                try
                {
                    var response = await s3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = obj.Key
                    }, cancellationToken);

                    await using var responseStream = response.ResponseStream;
                    await using var fileStream = File.Create(localPath);
                    await responseStream.CopyToAsync(fileStream, cancellationToken);

                    Interlocked.Add(ref totalBytes, obj.Size ?? 0);
                    var count = Interlocked.Increment(ref downloaded);

                    if (count % 10 == 0 || count == toDownload.Count)
                    {
                        Console.WriteLine($"  [{count}/{toDownload.Count}] Downloaded {obj.Key} ({FormatBytes(obj.Size ?? 0)}) — {FormatBytes(Interlocked.Read(ref totalBytes))} total");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errors);
                    Console.Error.WriteLine($"  Error downloading {obj.Key}: {ex.Message}");
                }
            });

        Console.WriteLine($"\nComplete: {downloaded} downloaded, {skipped} skipped, {errors} errors");
        Console.WriteLine($"Total downloaded: {FormatBytes(Interlocked.Read(ref totalBytes))}");
    }

    private static async Task<List<S3Object>> ListAllObjectsAsync(IAmazonS3 s3Client, string bucketName)
    {
        var objects = new List<S3Object>();
        string? continuationToken = null;

        do
        {
            var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName,
                ContinuationToken = continuationToken
            });

            objects.AddRange(response.S3Objects);
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (continuationToken is not null);

        return objects;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
