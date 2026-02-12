using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class UniformScaleTransformTests
{
    private readonly UniformScaleTransform _transform = new(minScale: 0.6, maxScale: 0.9);

    [Fact]
    public void Apply_ReducesBoundingBox()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.9, 0.5, 0.1, 0.9, 0.5],
            [0.1, 0.9, 0.5, 0.9, 0.1, 0.5]);

        var (_, _, origMaxX, _) = CompositionGeometry.ComputeBoundingBox(comp);
        var (_, _, origMinX, _) = CompositionGeometry.ComputeBoundingBox(comp);

        var result = _transform.Apply(comp, new Random(42));

        var (minX, minY, maxX, maxY) = CompositionGeometry.ComputeBoundingBox(result);
        var scaledWidth = maxX - minX;
        var originalWidth = 0.8; // 0.9 - 0.1

        scaledWidth.Should().BeLessThan(originalWidth);
    }

    [Fact]
    public void Apply_AllCoordinatesRemainInBounds()
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
                        x.Should().BeInRange(0.0, 1.0);
                    foreach (var y in stroke.Data[1])
                        y.Should().BeInRange(0.0, 1.0);
                }
            }
        }
    }

    [Fact]
    public void Apply_PreservesPointCount()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 4, pointsPerStroke: 10);

        var result = _transform.Apply(comp, new Random(42));

        CompositionGeometry.CountTotalPoints(result).Should().Be(40);
    }
}
