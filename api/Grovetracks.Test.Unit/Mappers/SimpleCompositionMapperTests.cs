using FluentAssertions;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Services;

namespace Grovetracks.Test.Unit.Mappers;

public class SimpleCompositionMapperTests
{
    private readonly SimpleCompositionMapper _mapper = new();

    private static QuickdrawSimpleDoodle CreateDoodle(
        string drawing,
        string word = "cat",
        string keyId = "test-key-1") => new()
    {
        KeyId = keyId,
        Word = word,
        CountryCode = "US",
        Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Recognized = true,
        Drawing = drawing
    };

    [Fact]
    public void SingleStroke_NormalizesCoordinatesToZeroOneRange()
    {
        var doodle = CreateDoodle("[[[0, 128, 255], [0, 64, 255]]]");

        var result = _mapper.MapToComposition(doodle);

        var strokeData = result.DoodleFragments[0].Strokes[0].Data;
        strokeData[0].Should().BeEquivalentTo(
            new[] { 0.0, Math.Round(128.0 / 255, 3), 1.0 });
        strokeData[1].Should().BeEquivalentTo(
            new[] { 0.0, Math.Round(64.0 / 255, 3), 1.0 });
    }

    [Fact]
    public void MultipleStrokes_AllNormalized()
    {
        var doodle = CreateDoodle("[[[10, 20], [30, 40]], [[100, 200], [150, 250]]]");

        var result = _mapper.MapToComposition(doodle);

        var strokes = result.DoodleFragments[0].Strokes;
        strokes.Should().HaveCount(2);

        strokes[0].Data[0][0].Should().Be(Math.Round(10.0 / 255, 3));
        strokes[1].Data[0][0].Should().Be(Math.Round(100.0 / 255, 3));
        strokes[0].Data[1][0].Should().Be(Math.Round(30.0 / 255, 3));
        strokes[1].Data[1][0].Should().Be(Math.Round(150.0 / 255, 3));
    }

    [Fact]
    public void WidthAndHeight_AreAlways255()
    {
        var doodle = CreateDoodle("[[[0, 50], [0, 50]]]");

        var result = _mapper.MapToComposition(doodle);

        result.Width.Should().Be(255);
        result.Height.Should().Be(255);
    }

    [Fact]
    public void Tags_ContainQuickdrawSimpleAndWord()
    {
        var doodle = CreateDoodle("[[[0, 100], [0, 100]]]", word: "fire truck");

        var result = _mapper.MapToComposition(doodle);

        result.Tags.Should().BeEquivalentTo(new[] { "quickdraw-simple", "fire truck" });
    }

    [Fact]
    public void StrokeStyleId_IsAlwaysZero()
    {
        var doodle = CreateDoodle("[[[0, 100], [0, 100]], [[50, 150], [50, 150]], [[10, 200], [10, 200]]]");

        var result = _mapper.MapToComposition(doodle);

        foreach (var stroke in result.DoodleFragments[0].Strokes)
        {
            stroke.Data[2].Should().BeEquivalentTo(new[] { 0.0 });
        }
    }

    [Fact]
    public void Coordinates_RoundedToThreeDecimalPlaces()
    {
        var doodle = CreateDoodle("[[[33], [66]]]");

        var result = _mapper.MapToComposition(doodle);

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        var ys = result.DoodleFragments[0].Strokes[0].Data[1];

        xs[0].Should().Be(Math.Round(33.0 / 255, 3));
        ys[0].Should().Be(Math.Round(66.0 / 255, 3));
    }

    [Fact]
    public void SingleDoodleFragment_WrapsAllStrokes()
    {
        var doodle = CreateDoodle("[[[0, 100], [0, 100]], [[50, 150], [50, 150]]]");

        var result = _mapper.MapToComposition(doodle);

        result.DoodleFragments.Should().HaveCount(1);
        result.DoodleFragments[0].Strokes.Should().HaveCount(2);
    }

    [Fact]
    public void ZeroCoordinate_MapsToZero()
    {
        var doodle = CreateDoodle("[[[0], [0]]]");

        var result = _mapper.MapToComposition(doodle);

        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(0.0);
        result.DoodleFragments[0].Strokes[0].Data[1][0].Should().Be(0.0);
    }

    [Fact]
    public void MaxCoordinate_MapsToOne()
    {
        var doodle = CreateDoodle("[[[255], [255]]]");

        var result = _mapper.MapToComposition(doodle);

        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(1.0);
        result.DoodleFragments[0].Strokes[0].Data[1][0].Should().Be(1.0);
    }
}
