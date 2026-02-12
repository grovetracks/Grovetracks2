using FluentAssertions;
using Grovetracks.Api.Controllers;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Grovetracks.Test.Unit.Controllers;

public class SimpleDoodlesControllerTests
{
    private readonly IQuickdrawSimpleDoodleRepository _mockRepository;
    private readonly ISimpleCompositionMapper _mockMapper;
    private readonly SimpleDoodlesController _controller;

    public SimpleDoodlesControllerTests()
    {
        _mockRepository = Substitute.For<IQuickdrawSimpleDoodleRepository>();
        _mockMapper = Substitute.For<ISimpleCompositionMapper>();
        _controller = new SimpleDoodlesController(_mockRepository, _mockMapper);
    }

    private static QuickdrawSimpleDoodle CreateDoodle(string word = "cat", string keyId = "test-key-1") => new()
    {
        KeyId = keyId,
        Word = word,
        CountryCode = "US",
        Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Recognized = true,
        Drawing = "[[[0, 128, 255], [0, 64, 255]]]"
    };

    private static Composition CreateComposition() => new()
    {
        Width = 255,
        Height = 255,
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
                            new List<double> { 0.0, 0.502, 1.0 }.AsReadOnly(),
                            new List<double> { 0.0, 0.251, 1.0 }.AsReadOnly(),
                            new List<double> { 0 }.AsReadOnly()
                        }.AsReadOnly()
                    }
                }.AsReadOnly()
            }
        }.AsReadOnly(),
        Tags = new List<string> { "quickdraw-simple", "cat" }.AsReadOnly()
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
        var doodles = new List<QuickdrawSimpleDoodle> { CreateDoodle() }.AsReadOnly();
        var composition = CreateComposition();

        _mockRepository.GetByWordAsync("cat", 24, Arg.Any<CancellationToken>())
            .Returns(doodles);
        _mockMapper.MapToComposition(Arg.Any<QuickdrawSimpleDoodle>())
            .Returns(composition);

        var result = await _controller.GetGalleryPage("cat");

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Doodle.KeyId.Should().Be("test-key-1");
        result.Items[0].Doodle.Word.Should().Be("cat");
        result.Items[0].Composition.Width.Should().Be(255);
        result.Items[0].Composition.Tags.Should().Contain("quickdraw-simple");
    }

    [Fact]
    public async Task GetGalleryPage_ClampsLimitToMaxPageSize()
    {
        _mockRepository.GetByWordAsync("cat", 48, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawSimpleDoodle>().AsReadOnly());

        await _controller.GetGalleryPage("cat", limit: 100);

        await _mockRepository.Received(1).GetByWordAsync("cat", 48, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGalleryPage_ClampsLimitMinimumToOne()
    {
        _mockRepository.GetByWordAsync("cat", 1, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawSimpleDoodle>().AsReadOnly());

        await _controller.GetGalleryPage("cat", limit: 0);

        await _mockRepository.Received(1).GetByWordAsync("cat", 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetComposition_WithValidKeyId_ReturnsComposition()
    {
        var doodle = CreateDoodle();
        var composition = CreateComposition();

        _mockRepository.GetByKeyIdAsync("test-key-1", Arg.Any<CancellationToken>())
            .Returns(doodle);
        _mockMapper.MapToComposition(doodle)
            .Returns(composition);

        var result = await _controller.GetComposition("test-key-1", CancellationToken.None);

        result.Value.Should().NotBeNull();
        result.Value!.Doodle.KeyId.Should().Be("test-key-1");
        result.Value.Composition.Width.Should().Be(255);
    }

    [Fact]
    public async Task GetComposition_WithInvalidKeyId_ReturnsNotFound()
    {
        _mockRepository.GetByKeyIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((QuickdrawSimpleDoodle?)null);

        var result = await _controller.GetComposition("nonexistent", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWordCount_ReturnsCountFromRepository()
    {
        _mockRepository.GetCountByWordAsync("cat", Arg.Any<CancellationToken>())
            .Returns(42);

        var result = await _controller.GetWordCount("cat", CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task GetGalleryPage_DoesNotLeakDrawingJsonInSummary()
    {
        var doodle = CreateDoodle();
        var composition = CreateComposition();

        _mockRepository.GetByWordAsync("cat", 24, Arg.Any<CancellationToken>())
            .Returns(new List<QuickdrawSimpleDoodle> { doodle }.AsReadOnly());
        _mockMapper.MapToComposition(Arg.Any<QuickdrawSimpleDoodle>())
            .Returns(composition);

        var result = await _controller.GetGalleryPage("cat");

        var summary = result.Items[0].Doodle;
        summary.Should().NotBeNull();
        summary.GetType().GetProperty("Drawing").Should().BeNull();
    }
}
