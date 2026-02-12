using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.Test.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.Test.Integration.Flows;

public class QuickdrawCompositionFlowTests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private AsyncServiceScope _scope;

    public QuickdrawCompositionFlowTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _scope = _fixture.ServiceProvider.CreateAsyncScope();
        var dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE quickdraw_doodles");
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }

    [Fact]
    public async Task FullFlow_SeedDbAndS3_ProducesCorrectComposition()
    {
        var dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = _scope.ServiceProvider.GetRequiredService<IQuickdrawDoodleRepository>();
        var mapper = _scope.ServiceProvider.GetRequiredService<ICompositionMapper>();

        var doodle = new QuickdrawDoodle
        {
            KeyId = "flow-test-key-1",
            Word = "fire truck",
            CountryCode = "US",
            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Recognized = true,
            DrawingReference = $"s3://{LocalStackFixture.TestBucketName}/flow-test-key-1"
        };

        dbContext.QuickdrawDoodles.Add(doodle);
        await dbContext.SaveChangesAsync();

        var drawingJson = "[[[0,120,240],[0,60,120]],[[30,90],[45,90]]]";
        await _fixture.LocalStack.S3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = LocalStackFixture.TestBucketName,
            Key = "flow-test-key-1",
            ContentBody = drawingJson,
            ContentType = "application/json"
        });

        var lookedUp = await repository.GetByKeyIdAsync("flow-test-key-1");
        lookedUp.Should().NotBeNull();

        var composition = await mapper.MapToCompositionAsync(lookedUp!);

        composition.Width.Should().Be(240);
        composition.Height.Should().Be(120);
        composition.Tags.Should().BeEquivalentTo(new[] { "quickdraw", "fire truck" });
        composition.DoodleFragments.Should().HaveCount(1);
        composition.DoodleFragments[0].Strokes.Should().HaveCount(2);

        var firstStroke = composition.DoodleFragments[0].Strokes[0];
        firstStroke.Data.Should().HaveCount(3);
        firstStroke.Data[0].Should().HaveCount(3);
        firstStroke.Data[1].Should().HaveCount(3);
        firstStroke.Data[2].Should().BeEquivalentTo(new[] { 0.0 });

        firstStroke.Data[0][0].Should().Be(0);
        firstStroke.Data[0][1].Should().Be(Math.Round(120.0 / (240 * 1.2), 3));
        firstStroke.Data[0][2].Should().Be(Math.Round(240.0 / (240 * 1.2), 3));
    }
}
