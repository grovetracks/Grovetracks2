using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Services;

public class S3DrawingStorageService(IAmazonS3 s3Client) : IDrawingStorageService
{
    public async Task<RawDrawing> GetDrawingAsync(
        string drawingReference,
        CancellationToken cancellationToken = default)
    {
        var (bucket, key) = ParseS3Uri(drawingReference);

        var request = new GetObjectRequest
        {
            BucketName = bucket,
            Key = key
        };

        var response = await s3Client.GetObjectAsync(request, cancellationToken);
        using var reader = new StreamReader(response.ResponseStream);
        var json = await reader.ReadToEndAsync(cancellationToken);

        var rawStrokes = JsonSerializer.Deserialize<List<List<List<double>>>>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize drawing from {drawingReference}");

        return new RawDrawing
        {
            Strokes = rawStrokes
                .Select(stroke => new RawStroke
                {
                    Xs = stroke[0].AsReadOnly(),
                    Ys = stroke[1].AsReadOnly()
                })
                .ToList()
                .AsReadOnly()
        };
    }

    private static (string Bucket, string Key) ParseS3Uri(string uri)
    {
        var parsed = new Uri(uri);
        return (parsed.Host, parsed.AbsolutePath.TrimStart('/'));
    }
}
