using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class StrokeRefinementTransformTests
{
    private readonly StrokeRefinementTransform _transform = new(multiplier: 3.0, minPoints: 10);

    [Fact]
    public void Apply_IncreasesPointCount()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.3, 0.5, 0.7, 0.9],
            [0.2, 0.4, 0.6, 0.4, 0.2]);

        var originalPoints = CompositionGeometry.CountTotalPoints(comp);

        var result = _transform.Apply(comp, new Random(42));

        CompositionGeometry.CountTotalPoints(result).Should().BeGreaterThan(originalPoints);
    }

    [Fact]
    public void Apply_AllCoordinatesInBounds()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var result = _transform.Apply(comp, new Random(42));

        foreach (var fragment in result.DoodleFragments)
        {
            foreach (var stroke in fragment.Strokes)
            {
                foreach (var x in stroke.Data[0])
                    x.Should().BeInRange(0.0, 1.0);
                foreach (var y in stroke.Data[1])
                    y.Should().BeInRange(0.0, 1.0);
            }
        }
    }

    [Fact]
    public void Apply_PreservesEndpoints()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.3, 0.5, 0.7, 0.9],
            [0.2, 0.4, 0.6, 0.4, 0.2]);

        var result = _transform.Apply(comp, new Random(42));

        var resultXs = result.DoodleFragments[0].Strokes[0].Data[0];
        var resultYs = result.DoodleFragments[0].Strokes[0].Data[1];

        resultXs[0].Should().Be(0.1);
        resultYs[0].Should().Be(0.2);
        resultXs[^1].Should().Be(0.9);
        resultYs[^1].Should().Be(0.2);
    }

    [Fact]
    public void Apply_ProducesApproximatelyUniformSpacing()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.0, 0.5, 1.0],
            [0.0, 0.0, 0.0]);

        var result = _transform.Apply(comp, new Random(42));

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];

        var spacings = new List<double>();
        for (var i = 1; i < xs.Count; i++)
            spacings.Add(Math.Abs(xs[i] - xs[i - 1]));

        var avgSpacing = spacings.Average();
        foreach (var spacing in spacings)
            spacing.Should().BeApproximately(avgSpacing, 0.05);
    }

    [Fact]
    public void Apply_RespectsMinimumPoints()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.2, 0.8],
            [0.3, 0.7]);

        var result = _transform.Apply(comp, new Random(42));

        result.DoodleFragments[0].Strokes[0].Data[0].Count.Should().BeGreaterThanOrEqualTo(10);
    }
}
