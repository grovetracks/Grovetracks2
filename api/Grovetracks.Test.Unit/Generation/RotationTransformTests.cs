using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class RotationTransformTests
{
    private readonly RotationTransform _transform = new(maxDegrees: 15.0);

    [Fact]
    public void Apply_CenterPointRemainsAtCenter()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.5, 0.1, 0.9, 0.5, 0.5, 0.5],
            [0.5, 0.1, 0.9, 0.1, 0.9, 0.5]);

        var result = _transform.Apply(comp, new Random(42));

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        var ys = result.DoodleFragments[0].Strokes[0].Data[1];

        xs[0].Should().Be(0.5);
        ys[0].Should().Be(0.5);
    }

    [Fact]
    public void Apply_AllCoordinatesRemainInBounds()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 5, pointsPerStroke: 20);

        for (var seed = 0; seed < 10; seed++)
        {
            var result = _transform.Apply(comp, new Random(seed));

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
    }

    [Fact]
    public void Apply_DifferentSeedsProduceDifferentResults()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var result1 = _transform.Apply(comp, new Random(1));
        var result2 = _transform.Apply(comp, new Random(999));

        var xs1 = result1.DoodleFragments[0].Strokes[0].Data[0];
        var xs2 = result2.DoodleFragments[0].Strokes[0].Data[0];

        xs1.Should().NotBeEquivalentTo(xs2);
    }

    [Fact]
    public void Apply_PreservesStrokeCount()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 5);

        var result = _transform.Apply(comp, new Random(42));

        CompositionGeometry.CountTotalStrokes(result).Should().Be(5);
    }
}
