using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class StrokeElaborationTransformTests
{
    private readonly StrokeElaborationTransform _transform = new();

    [Fact]
    public void Apply_AddsParallelStrokes()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 3);

        var originalStrokes = CompositionGeometry.CountTotalStrokes(comp);

        var result = _transform.Apply(comp, new Random(42));

        CompositionGeometry.CountTotalStrokes(result).Should().BeGreaterThan(originalStrokes);
    }

    [Fact]
    public void Apply_AllCoordinatesInBounds()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        for (var seed = 0; seed < 10; seed++)
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
    public void Apply_PreservesOriginalStrokes()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.2, 0.5, 0.8],
            [0.3, 0.6, 0.3]);

        var result = _transform.Apply(comp, new Random(42));

        var resultXs = result.DoodleFragments[0].Strokes[0].Data[0];
        resultXs[0].Should().Be(0.2);
        resultXs[1].Should().Be(0.5);
        resultXs[2].Should().Be(0.8);
    }

    [Fact]
    public void Apply_DifferentSeedsProduceDifferentOffsets()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 3);

        var result1 = _transform.Apply(comp, new Random(1));
        var result2 = _transform.Apply(comp, new Random(999));

        var strokes1 = CompositionGeometry.CountTotalStrokes(result1);
        var strokes2 = CompositionGeometry.CountTotalStrokes(result2);

        (strokes1 != strokes2 ||
         result1.DoodleFragments[0].Strokes[^1].Data[0][0] !=
         result2.DoodleFragments[0].Strokes[^1].Data[0][0])
            .Should().BeTrue();
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
}
