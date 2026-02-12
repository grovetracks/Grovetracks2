using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.Etl.Mappers;
using Grovetracks.Etl.Models;

namespace Grovetracks.Test.Unit.Generation;

public class AiCompositionMapperTests
{
    [Fact]
    public void MapToComposition_ValidInput_ReturnsCorrectStructure()
    {
        var source = CreateAiComposition("cat",
            (new[] { 0.1, 0.3, 0.5 }, new[] { 0.2, 0.4, 0.6 }),
            (new[] { 0.6, 0.8 }, new[] { 0.7, 0.9 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        result.Width.Should().Be(255);
        result.Height.Should().Be(255);
        result.DoodleFragments.Should().HaveCount(1);
        result.DoodleFragments[0].Strokes.Should().HaveCount(2);
        result.DoodleFragments[0].Strokes[0].Data[0].Should().HaveCount(3);
        result.DoodleFragments[0].Strokes[1].Data[0].Should().HaveCount(2);
    }

    [Fact]
    public void MapToComposition_ClampsOutOfBoundsCoordinates()
    {
        var source = CreateAiComposition("test",
            (new[] { -0.5, 1.5, 0.5 }, new[] { -0.1, 0.5, 2.0 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        var ys = result.DoodleFragments[0].Strokes[0].Data[1];

        xs[0].Should().Be(0.0);
        xs[1].Should().Be(1.0);
        xs[2].Should().Be(0.5);
        ys[0].Should().Be(0.0);
        ys[2].Should().Be(1.0);
    }

    [Fact]
    public void MapToComposition_RoundsToThreeDecimals()
    {
        var source = CreateAiComposition("test",
            (new[] { 0.12345, 0.67891 }, new[] { 0.11111, 0.99999 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        var ys = result.DoodleFragments[0].Strokes[0].Data[1];

        xs[0].Should().Be(0.123);
        xs[1].Should().Be(0.679);
        ys[0].Should().Be(0.111);
        ys[1].Should().Be(1.0);
    }

    [Fact]
    public void MapToComposition_FiltersStrokesWithTooFewPoints()
    {
        var source = new AiComposition
        {
            Subject = "test",
            Strokes = new List<AiStroke>
            {
                new() { Xs = new[] { 0.1 }, Ys = new[] { 0.2 } },
                new() { Xs = new[] { 0.3, 0.5 }, Ys = new[] { 0.4, 0.6 } },
                new() { Xs = Array.Empty<double>(), Ys = Array.Empty<double>() }
            }.AsReadOnly()
        };

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        result.DoodleFragments[0].Strokes.Should().HaveCount(1);
        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(0.3);
    }

    [Fact]
    public void MapToComposition_FiltersMismatchedXsYsLengths()
    {
        var source = new AiComposition
        {
            Subject = "test",
            Strokes = new List<AiStroke>
            {
                new() { Xs = new[] { 0.1, 0.2, 0.3 }, Ys = new[] { 0.4, 0.5 } },
                new() { Xs = new[] { 0.6, 0.7 }, Ys = new[] { 0.8, 0.9 } }
            }.AsReadOnly()
        };

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        result.DoodleFragments[0].Strokes.Should().HaveCount(1);
        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(0.6);
    }

    [Fact]
    public void MapToComposition_SetsTimingDataToZero()
    {
        var source = CreateAiComposition("test",
            (new[] { 0.1, 0.5, 0.9 }, new[] { 0.2, 0.5, 0.8 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        var stroke = result.DoodleFragments[0].Strokes[0];
        stroke.Data.Should().HaveCount(3);
        stroke.Data[2].Should().HaveCount(1);
        stroke.Data[2][0].Should().Be(0);
    }

    [Fact]
    public void MapToComposition_SetsCorrectTags_ClaudeSonnet()
    {
        var source = CreateAiComposition("bicycle",
            (new[] { 0.1, 0.9 }, new[] { 0.2, 0.8 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        result.Tags.Should().BeEquivalentTo(new[] { "ai-generated", "claude-sonnet", "bicycle" });
    }

    [Fact]
    public void MapToComposition_SetsCorrectTags_OllamaModel()
    {
        var source = CreateAiComposition("tree",
            (new[] { 0.2, 0.8 }, new[] { 0.1, 0.9 }));

        var result = AiCompositionMapper.MapToComposition(source, "ollama-qwen2.5:14b");

        result.Tags.Should().BeEquivalentTo(new[] { "ai-generated", "ollama-qwen2.5:14b", "tree" });
    }

    [Fact]
    public void MapToComposition_EmptyStrokes_ReturnsCompositionWithEmptyFragment()
    {
        var source = new AiComposition
        {
            Subject = "test",
            Strokes = new List<AiStroke>().AsReadOnly()
        };

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        result.DoodleFragments.Should().HaveCount(1);
        result.DoodleFragments[0].Strokes.Should().BeEmpty();
    }

    [Fact]
    public void MapToComposition_MultipleStrokes_PreservesOrder()
    {
        var source = CreateAiComposition("test",
            (new[] { 0.1, 0.2 }, new[] { 0.3, 0.4 }),
            (new[] { 0.5, 0.6 }, new[] { 0.7, 0.8 }),
            (new[] { 0.9, 0.95 }, new[] { 0.1, 0.15 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        result.DoodleFragments[0].Strokes.Should().HaveCount(3);
        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(0.1);
        result.DoodleFragments[0].Strokes[1].Data[0][0].Should().Be(0.5);
        result.DoodleFragments[0].Strokes[2].Data[0][0].Should().Be(0.9);
    }

    [Fact]
    public void MapToComposition_ValidResult_PassesCompositionValidator()
    {
        var source = CreateAiComposition("cat",
            (new[] { 0.1, 0.2, 0.3, 0.4, 0.5 }, new[] { 0.1, 0.3, 0.5, 0.7, 0.9 }),
            (new[] { 0.5, 0.6, 0.7, 0.8, 0.9 }, new[] { 0.9, 0.7, 0.5, 0.3, 0.1 }),
            (new[] { 0.2, 0.4, 0.6 }, new[] { 0.4, 0.6, 0.4 }));

        var result = AiCompositionMapper.MapToComposition(source, "claude-sonnet");

        var (isValid, qualityScore) = CompositionValidator.Validate(result);
        isValid.Should().BeTrue();
        qualityScore.Should().BeGreaterThan(0);
    }

    private static AiComposition CreateAiComposition(
        string subject,
        params (double[] Xs, double[] Ys)[] strokes)
    {
        return new AiComposition
        {
            Subject = subject,
            Strokes = strokes
                .Select(s => new AiStroke
                {
                    Xs = s.Xs.ToList().AsReadOnly(),
                    Ys = s.Ys.ToList().AsReadOnly()
                })
                .ToList()
                .AsReadOnly()
        };
    }
}
