using FluentAssertions;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;
using Grovetracks.Test.Unit.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Grovetracks.Test.Unit.Mappers;

public class QuickdrawCompositionMapperTests : IClassFixture<UnitTestFixture>
{
    private readonly ICompositionMapper _mapper;
    private readonly IDrawingStorageService _mockStorage;

    public QuickdrawCompositionMapperTests(UnitTestFixture fixture)
    {
        _mapper = fixture.ServiceProvider.GetRequiredService<ICompositionMapper>();
        _mockStorage = fixture.MockDrawingStorageService;
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

    private static RawDrawing CreateRawDrawing(params (double[] xs, double[] ys)[] strokes) => new()
    {
        Strokes = strokes.Select(s => new RawStroke
        {
            Xs = s.xs.ToList().AsReadOnly(),
            Ys = s.ys.ToList().AsReadOnly()
        }).ToList().AsReadOnly()
    };

    [Fact]
    public async Task SingleStroke_NormalizesCoordinatesCorrectly()
    {
        var doodle = CreateDoodle();
        var rawDrawing = CreateRawDrawing(
            ([0, 100, 200], [0, 50, 100]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        var strokeData = result.DoodleFragments[0].Strokes[0].Data;
        strokeData[0].Should().BeEquivalentTo(
            new[] { 0.0, Math.Round(100.0 / (200 * 1.2), 3), Math.Round(200.0 / (200 * 1.2), 3) });
        strokeData[1].Should().BeEquivalentTo(
            new[] { 0.0, Math.Round(50.0 / (100 * 1.2), 3), Math.Round(100.0 / (100 * 1.2), 3) });
    }

    [Fact]
    public async Task MultipleStrokes_NormalizedUsingGlobalMax()
    {
        var doodle = CreateDoodle();
        var rawDrawing = CreateRawDrawing(
            ([10, 20], [10, 20]),
            ([50, 300], [50, 200]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        var strokes = result.DoodleFragments[0].Strokes;
        strokes.Should().HaveCount(2);

        strokes[0].Data[0][0].Should().Be(Math.Round(10.0 / (300 * 1.2), 3));
        strokes[1].Data[0][0].Should().Be(Math.Round(50.0 / (300 * 1.2), 3));

        strokes[0].Data[1][0].Should().Be(Math.Round(10.0 / (200 * 1.2), 3));
        strokes[1].Data[1][0].Should().Be(Math.Round(50.0 / (200 * 1.2), 3));
    }

    [Fact]
    public async Task WidthAndHeight_AreTruncatedNotRounded()
    {
        var doodle = CreateDoodle();
        var rawDrawing = CreateRawDrawing(
            ([0, 255.9], [0, 100.7]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        result.Width.Should().Be(255);
        result.Height.Should().Be(100);
    }

    [Fact]
    public async Task Tags_ContainQuickdrawAndWordWithSpaces()
    {
        var doodle = CreateDoodle(word: "fire truck");
        var rawDrawing = CreateRawDrawing(([0, 100], [0, 100]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        result.Tags.Should().BeEquivalentTo(new[] { "quickdraw", "fire truck" });
    }

    [Fact]
    public async Task StrokeStyleId_IsAlwaysZero()
    {
        var doodle = CreateDoodle();
        var rawDrawing = CreateRawDrawing(
            ([0, 100], [0, 100]),
            ([50, 150], [50, 150]),
            ([10, 200], [10, 200]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        foreach (var stroke in result.DoodleFragments[0].Strokes)
        {
            stroke.Data[2].Should().BeEquivalentTo(new[] { 0.0 });
        }
    }

    [Fact]
    public async Task Coordinates_RoundedToThreeDecimalPlaces()
    {
        var doodle = CreateDoodle();
        var rawDrawing = CreateRawDrawing(
            ([33.333333], [66.666666]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        var ys = result.DoodleFragments[0].Strokes[0].Data[1];

        // maxX = 33.333333, x / (maxX * 1.2) = 33.333333 / 39.9999996 ≈ 0.833
        xs[0].Should().Be(Math.Round(33.333333 / (33.333333 * 1.2), 3));

        // maxY = 66.666666, y / (maxY * 1.2) = 66.666666 / 79.9999992 ≈ 0.833
        ys[0].Should().Be(Math.Round(66.666666 / (66.666666 * 1.2), 3));
    }

    [Fact]
    public async Task SingleDoodleFragment_WrapsAllStrokes()
    {
        var doodle = CreateDoodle();
        var rawDrawing = CreateRawDrawing(
            ([0, 100], [0, 100]),
            ([50, 150], [50, 150]));

        _mockStorage.GetDrawingAsync(doodle.DrawingReference, Arg.Any<CancellationToken>())
            .Returns(rawDrawing);

        var result = await _mapper.MapToCompositionAsync(doodle);

        result.DoodleFragments.Should().HaveCount(1);
        result.DoodleFragments[0].Strokes.Should().HaveCount(2);
    }
}
