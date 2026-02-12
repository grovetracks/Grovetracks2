using FluentAssertions;
using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.Test.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.Test.Integration.Repositories;

public class QuickdrawDoodleRepositoryTests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private AsyncServiceScope _scope;
    private IQuickdrawDoodleRepository _repository = null!;
    private AppDbContext _dbContext = null!;

    public QuickdrawDoodleRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _scope = _fixture.ServiceProvider.CreateAsyncScope();
        _repository = _scope.ServiceProvider.GetRequiredService<IQuickdrawDoodleRepository>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE quickdraw_doodles");
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }

    private async Task SeedDoodlesAsync(params QuickdrawDoodle[] doodles)
    {
        _dbContext.QuickdrawDoodles.AddRange(doodles);
        await _dbContext.SaveChangesAsync();
    }

    private static QuickdrawDoodle CreateDoodle(string keyId, string word, bool recognized = true) => new()
    {
        KeyId = keyId,
        Word = word,
        CountryCode = "US",
        Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Recognized = recognized,
        DrawingReference = $"s3://test-bucket/{keyId}"
    };

    [Fact]
    public async Task GetByWordAsync_ReturnsMatchingDoodles()
    {
        await SeedDoodlesAsync(
            CreateDoodle("key-1", "cat"),
            CreateDoodle("key-2", "cat"),
            CreateDoodle("key-3", "dog"));

        var results = await _repository.GetByWordAsync("cat");

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(d => d.Word.Should().Be("cat"));
    }

    [Fact]
    public async Task GetByWordAsync_WithLimit_ConstrainsResults()
    {
        await SeedDoodlesAsync(
            CreateDoodle("key-1", "airplane"),
            CreateDoodle("key-2", "airplane"),
            CreateDoodle("key-3", "airplane"));

        var results = await _repository.GetByWordAsync("airplane", 2);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByKeyIdAsync_ReturnsExactMatch()
    {
        await SeedDoodlesAsync(CreateDoodle("key-42", "flower"));

        var result = await _repository.GetByKeyIdAsync("key-42");

        result.Should().NotBeNull();
        result!.KeyId.Should().Be("key-42");
        result.Word.Should().Be("flower");
    }

    [Fact]
    public async Task GetByKeyIdAsync_ReturnsNullForNonexistent()
    {
        var result = await _repository.GetByKeyIdAsync("nonexistent-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDistinctWordsAsync_ReturnsSortedUniqueWords()
    {
        await SeedDoodlesAsync(
            CreateDoodle("key-1", "zebra"),
            CreateDoodle("key-2", "cat"),
            CreateDoodle("key-3", "cat"),
            CreateDoodle("key-4", "airplane"));

        var results = await _repository.GetDistinctWordsAsync();

        results.Should().BeEquivalentTo(new[] { "airplane", "cat", "zebra" },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetByWordAsync_ForNonexistentWord_ReturnsEmptyList()
    {
        await SeedDoodlesAsync(CreateDoodle("key-1", "cat"));

        var results = await _repository.GetByWordAsync("nonexistent");

        results.Should().BeEmpty();
    }
}
