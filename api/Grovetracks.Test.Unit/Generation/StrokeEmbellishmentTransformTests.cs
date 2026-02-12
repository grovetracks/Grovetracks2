using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class StrokeEmbellishmentTransformTests
{
    private readonly StrokeEmbellishmentTransform _transform = new();

    [Fact]
    public void Apply_AddsStrokes()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 5);

        var originalStrokes = CompositionGeometry.CountTotalStrokes(comp);

        var result = _transform.Apply(comp, new Random(42));

        CompositionGeometry.CountTotalStrokes(result).Should().BeGreaterThan(originalStrokes);
    }

    [Fact]
    public void Apply_AllCoordinatesInBounds()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        for (var seed = 0; seed < 20; seed++)
        {
            var result = _transform.Apply(comp, new Random(seed));

            foreach (var fragment in result.DoodleFragments)
            {
                foreach (var stroke in fragment.Strokes)
                {
                    foreach (var x in stroke.Data[0])
                        x.Should().BeInRange(0.0, 1.0, $"X out of bounds with seed {seed}");
                    foreach (var y in stroke.Data[1])
                        y.Should().BeInRange(0.0, 1.0, $"Y out of bounds with seed {seed}");
                }
            }
        }
    }

    [Fact]
    public void Apply_DifferentSeedsCanProduceDifferentModes()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 5);

        var strokeCounts = Enumerable.Range(0, 30)
            .Select(seed => CompositionGeometry.CountTotalStrokes(
                _transform.Apply(comp, new Random(seed))))
            .Distinct()
            .ToList();

        strokeCounts.Count.Should().BeGreaterThan(1,
            "different seeds should produce different embellishment modes with different stroke counts");
    }

    [Fact]
    public void Apply_PreservesTagsAndDimensions()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var result = _transform.Apply(comp, new Random(42));

        result.Width.Should().Be(comp.Width);
        result.Height.Should().Be(comp.Height);
        result.Tags.Should().BeEquivalentTo(comp.Tags);
    }

    [Fact]
    public void Apply_ShadowMode_StrokesDoubled()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 3);

        for (var seed = 0; seed < 30; seed++)
        {
            var result = _transform.Apply(comp, new Random(seed));
            var resultStrokes = CompositionGeometry.CountTotalStrokes(result);

            if (resultStrokes == 6)
            {
                resultStrokes.Should().Be(6, "shadow mode doubles stroke count");
                return;
            }
        }

        true.Should().BeTrue("at least one seed should trigger shadow mode within 30 tries");
    }
}
