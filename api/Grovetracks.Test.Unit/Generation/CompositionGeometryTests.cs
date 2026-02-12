using FluentAssertions;
using Grovetracks.DataAccess.Generation;

namespace Grovetracks.Test.Unit.Generation;

public class CompositionGeometryTests
{
    [Fact]
    public void ComputeBoundingBox_ReturnsCorrectBounds()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.5, 0.9],
            [0.2, 0.6, 0.8]);

        var (minX, minY, maxX, maxY) = CompositionGeometry.ComputeBoundingBox(comp);

        minX.Should().Be(0.1);
        minY.Should().Be(0.2);
        maxX.Should().Be(0.9);
        maxY.Should().Be(0.8);
    }

    [Fact]
    public void ClampCoordinate_ClampsToRange()
    {
        CompositionGeometry.ClampCoordinate(-0.5).Should().Be(0.0);
        CompositionGeometry.ClampCoordinate(0.5).Should().Be(0.5);
        CompositionGeometry.ClampCoordinate(1.5).Should().Be(1.0);
    }

    [Fact]
    public void TransformComposition_AppliesTransformToAllPoints()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.5],
            [0.2, 0.6]);

        var result = CompositionGeometry.TransformComposition(comp, (x, y) =>
            (Math.Round(x * 2, 3), Math.Round(y * 2, 3)));

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        var ys = result.DoodleFragments[0].Strokes[0].Data[1];

        xs[0].Should().Be(0.2);
        xs[1].Should().Be(1.0);
        ys[0].Should().Be(0.4);
        ys[1].Should().Be(1.2);
    }

    [Fact]
    public void TransformComposition_PreservesTags()
    {
        var comp = TestCompositionFactory.CreateSimple([0.5], [0.5]);

        var result = CompositionGeometry.TransformComposition(comp, (x, y) => (x, y));

        result.Tags.Should().BeEquivalentTo(comp.Tags);
    }

    [Fact]
    public void CountTotalPoints_ReturnsCorrectCount()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 3, pointsPerStroke: 10);

        CompositionGeometry.CountTotalPoints(comp).Should().Be(30);
    }

    [Fact]
    public void CountTotalStrokes_ReturnsCorrectCount()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 5);

        CompositionGeometry.CountTotalStrokes(comp).Should().Be(5);
    }

    [Fact]
    public void PlaceInRegion_ScalesAndCentersInRegion()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.0, 1.0],
            [0.0, 1.0]);

        var result = CompositionGeometry.PlaceInRegion(comp, 0.0, 0.0, 0.5, 0.5, 1.0);

        var (minX, minY, maxX, maxY) = CompositionGeometry.ComputeBoundingBox(result);

        maxX.Should().BeLessThanOrEqualTo(0.5);
        maxY.Should().BeLessThanOrEqualTo(0.5);
        minX.Should().BeGreaterThanOrEqualTo(0.0);
        minY.Should().BeGreaterThanOrEqualTo(0.0);
    }
}
