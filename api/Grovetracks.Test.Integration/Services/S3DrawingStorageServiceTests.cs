using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.Test.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.Test.Integration.Services;

public class S3DrawingStorageServiceTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IDrawingStorageService _storageService;
    private readonly IAmazonS3 _s3Client;

    public S3DrawingStorageServiceTests(IntegrationTestFixture fixture)
    {
        _storageService = fixture.ServiceProvider.GetRequiredService<IDrawingStorageService>();
        _s3Client = fixture.LocalStack.S3Client;
    }

    private async Task UploadDrawingAsync(string key, string json)
    {
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = LocalStackFixture.TestBucketName,
            Key = key,
            ContentBody = json,
            ContentType = "application/json"
        });
    }

    [Fact]
    public async Task GetDrawingAsync_ReturnsDeserializedRawDrawing()
    {
        var drawingJson = "[[[0,100,200],[0,50,100]],[[10,20],[30,40]]]";
        await UploadDrawingAsync("test-drawing-1", drawingJson);

        var result = await _storageService.GetDrawingAsync(
            $"s3://{LocalStackFixture.TestBucketName}/test-drawing-1");

        result.Strokes.Should().HaveCount(2);
        result.Strokes[0].Xs.Should().BeEquivalentTo(new[] { 0.0, 100, 200 });
        result.Strokes[0].Ys.Should().BeEquivalentTo(new[] { 0.0, 50, 100 });
        result.Strokes[1].Xs.Should().BeEquivalentTo(new[] { 10.0, 20 });
        result.Strokes[1].Ys.Should().BeEquivalentTo(new[] { 30.0, 40 });
    }

    [Fact]
    public async Task GetDrawingAsync_WithNonexistentKey_Throws()
    {
        var act = () => _storageService.GetDrawingAsync(
            $"s3://{LocalStackFixture.TestBucketName}/nonexistent-key");

        await act.Should().ThrowAsync<AmazonS3Exception>();
    }

    [Fact]
    public async Task GetDrawingAsync_SingleStroke_ParsesCorrectly()
    {
        var drawingJson = "[[[10,20,30],[40,50,60]]]";
        await UploadDrawingAsync("test-single-stroke", drawingJson);

        var result = await _storageService.GetDrawingAsync(
            $"s3://{LocalStackFixture.TestBucketName}/test-single-stroke");

        result.Strokes.Should().HaveCount(1);
        result.Strokes[0].Xs.Should().HaveCount(3);
        result.Strokes[0].Ys.Should().HaveCount(3);
    }
}
