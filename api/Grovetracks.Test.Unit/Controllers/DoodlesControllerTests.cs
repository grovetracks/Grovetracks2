using FluentAssertions;
using Grovetracks.Api.Controllers;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Grovetracks.Test.Unit.Controllers;

public class DoodlesControllerTests
{
    private readonly IQuickdrawDoodleRepository _mockRepository;
    private readonly ICompositionMapper _mockMapper;
    private readonly IDoodleEngagementRepository _mockEngagementRepository;
    private readonly DoodlesController _controller;

    public DoodlesControllerTests()
    {
        _mockRepository = Substitute.For<IQuickdrawDoodleRepository>();
        _mockMapper = Substitute.For<ICompositionMapper>();
        _mockEngagementRepository = Substitute.For<IDoodleEngagementRepository>();
        _controller = new DoodlesController(_mockRepository, _mockMapper, _mockEngagementRepository);
    }

    private static QuickdrawDoodle CreateDoodle(string word = "cat", string keyId = "test-key-1") => new()
    {
        KeyId = keyId,
        Word = word,
        CountryCode = "US",
        Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Recognized = true,
        DrawingReference = $"s3://test-bucket/{keyId}"
    };

    private static Composition CreateComposition(int width = 200, int height = 100) => new()
    {
        Width = width,
        Height = height,
        DoodleFragments = new List<DoodleFragment>
        {
            new()
            {
                Strokes = new List<Stroke>
                {
                    new()
                    {
                        Data = new List<IReadOnlyList<double>>
                        {
                            new List<double> { 0.0, 0.5, 1.0 }.AsReadOnly(),
                            new List<double> { 0.0, 0.5, 1.0 }.AsReadOnly(),
                            new List<double> { 0 }.AsReadOnly()
                        }.AsReadOnly()
                    }
                }.AsReadOnly()
            }
        }.AsReadOnly(),
        Tags = new List<string> { "quickdraw", "cat" }.AsReadOnly()
    };

    [Fact]
    public async Task GetDistinctWords_ReturnsWordsFromRepository()
    {
        var expected = new List<string> { "apple", "cat", "dog" }.AsReadOnly();
        _mockRepository.GetDistinctWordsAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _controller.GetDistinctWords(CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetGalleryPage_ReturnsDoodlesWithCompositions()
    {
        var doodles = new List<QuickdrawDoodle> { CreateDoodle() }.AsReadOnly();
        var composition = CreateComposition();

        _mockRepository.GetByWordAsync("cat", 25, Arg.Any<CancellationToken>())
            .Returns(doodles);
        _mockMapper.MapToCompositionAsync(Arg.Any<QuickdrawDoodle>(), Arg.Any<CancellationToken>())
            .Returns(composition);

        var result = await _controller.GetGalleryPage("cat");

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.HasMore.Should().BeFalse();
        result.Items[0].Doodle.KeyId.Should().Be("test-key-1");
        result.Items[0].Doodle.Word.Should().Be("cat");
        result.Items[0].Composition.Width.Should().Be(200);
        result.Items[0].Composition.Tags.Should().Contain("quickdraw");
    }

    [Fact]
    public async Task GetGalleryPage_ClampsLimitToMaxPageSize()
    {
        _mockRepository.GetByWordAsync("cat", 73, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawDoodle>().AsReadOnly());

        await _controller.GetGalleryPage("cat", limit: 100);

        await _mockRepository.Received(1).GetByWordAsync("cat", 73, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGalleryPage_ClampsLimitMinimumToOne()
    {
        _mockRepository.GetByWordAsync("cat", 2, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawDoodle>().AsReadOnly());

        await _controller.GetGalleryPage("cat", limit: 0);

        await _mockRepository.Received(1).GetByWordAsync("cat", 2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGalleryPage_SetsHasMoreWhenMoreResultsExist()
    {
        var doodles = Enumerable.Range(0, 25)
            .Select(i => CreateDoodle(keyId: $"key-{i}"))
            .ToList()
            .AsReadOnly();
        var composition = CreateComposition();

        _mockRepository.GetByWordAsync("cat", 25, Arg.Any<CancellationToken>())
            .Returns(doodles);
        _mockMapper.MapToCompositionAsync(Arg.Any<QuickdrawDoodle>(), Arg.Any<CancellationToken>())
            .Returns(composition);

        var result = await _controller.GetGalleryPage("cat");

        result.HasMore.Should().BeTrue();
        result.Items.Should().HaveCount(24);
    }

    [Fact]
    public async Task GetComposition_WithValidKeyId_ReturnsComposition()
    {
        var doodle = CreateDoodle();
        var composition = CreateComposition();

        _mockRepository.GetByKeyIdAsync("test-key-1", Arg.Any<CancellationToken>())
            .Returns(doodle);
        _mockMapper.MapToCompositionAsync(doodle, Arg.Any<CancellationToken>())
            .Returns(composition);

        var result = await _controller.GetComposition("test-key-1", CancellationToken.None);

        result.Value.Should().NotBeNull();
        result.Value!.Doodle.KeyId.Should().Be("test-key-1");
        result.Value.Composition.Width.Should().Be(200);
    }

    [Fact]
    public async Task GetComposition_WithInvalidKeyId_ReturnsNotFound()
    {
        _mockRepository.GetByKeyIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((QuickdrawDoodle?)null);

        var result = await _controller.GetComposition("nonexistent", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetGalleryPage_ExcludesDrawingReferenceFromResponse()
    {
        var doodle = CreateDoodle();
        var composition = CreateComposition();

        _mockRepository.GetByWordAsync("cat", 25, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawDoodle> { doodle }.AsReadOnly());
        _mockMapper.MapToCompositionAsync(Arg.Any<QuickdrawDoodle>(), Arg.Any<CancellationToken>())
            .Returns(composition);

        var result = await _controller.GetGalleryPage("cat");

        var summary = result.Items[0].Doodle;
        summary.Should().NotBeNull();
        summary.GetType().GetProperty("DrawingReference").Should().BeNull();
    }

    [Fact]
    public async Task GetGalleryPage_ExcludeEngaged_CallsExcludingRepository()
    {
        var engagedKeys = new HashSet<string> { "engaged-1", "engaged-2" } as IReadOnlySet<string>;
        _mockEngagementRepository.GetEngagedKeyIdsAsync(Arg.Any<CancellationToken>())
            .Returns(engagedKeys);
        _mockRepository.GetByWordExcludingKeysAsync("cat", 25, engagedKeys, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawDoodle>().AsReadOnly());

        await _controller.GetGalleryPage("cat", excludeEngaged: true);

        await _mockRepository.Received(1).GetByWordExcludingKeysAsync(
            "cat", 25, engagedKeys, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGalleryPage_WithoutExcludeEngaged_CallsStandardRepository()
    {
        _mockRepository.GetByWordAsync("cat", 25, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawDoodle>().AsReadOnly());

        await _controller.GetGalleryPage("cat", excludeEngaged: false);

        await _mockRepository.Received(1).GetByWordAsync("cat", 25, Arg.Any<CancellationToken>());
        await _mockEngagementRepository.DidNotReceive().GetEngagedKeyIdsAsync(Arg.Any<CancellationToken>());
    }
}
