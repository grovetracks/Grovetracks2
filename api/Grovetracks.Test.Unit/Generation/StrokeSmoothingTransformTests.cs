using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class StrokeSmoothingTransformTests
{
    private readonly StrokeSmoothingTransform _transform = new(resolution: 3);

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
    public void Apply_PreservesFragmentAndTagStructure()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var result = _transform.Apply(comp, new Random(42));

        result.DoodleFragments.Count.Should().Be(comp.DoodleFragments.Count);
        result.DoodleFragments[0].Strokes.Count.Should().Be(comp.DoodleFragments[0].Strokes.Count);
        result.Tags.Should().BeEquivalentTo(comp.Tags);
    }

    [Fact]
    public void Apply_ShortStrokesUnchanged()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.3, 0.7],
            [0.4, 0.6]);

        var result = _transform.Apply(comp, new Random(42));

        result.DoodleFragments[0].Strokes[0].Data[0].Count.Should().Be(2);
    }

    [Fact]
    public void Apply_HigherResolutionProducesMorePoints()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.3, 0.5, 0.7, 0.9],
            [0.2, 0.4, 0.6, 0.4, 0.2]);

        var lowRes = new StrokeSmoothingTransform(resolution: 2);
        var highRes = new StrokeSmoothingTransform(resolution: 5);

        var lowResult = lowRes.Apply(comp, new Random(42));
        var highResult = highRes.Apply(comp, new Random(42));

        CompositionGeometry.CountTotalPoints(highResult)
            .Should().BeGreaterThan(CompositionGeometry.CountTotalPoints(lowResult));
    }
}
