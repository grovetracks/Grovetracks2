using FluentAssertions;
using Grovetracks.DataAccess.Generation.Transforms;

namespace Grovetracks.Test.Unit.Generation;

public class HorizontalMirrorTransformTests
{
    private readonly HorizontalMirrorTransform _transform = new();
    private readonly Random _rng = new(42);

    [Fact]
    public void Apply_FlipsXCoordinates()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.3, 0.9],
            [0.2, 0.5, 0.8]);

        var result = _transform.Apply(comp, _rng);

        var xs = result.DoodleFragments[0].Strokes[0].Data[0];
        xs[0].Should().Be(0.9);
        xs[1].Should().Be(0.7);
        xs[2].Should().Be(0.1);
    }

    [Fact]
    public void Apply_PreservesYCoordinates()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.1, 0.5],
            [0.2, 0.8]);

        var result = _transform.Apply(comp, _rng);

        var ys = result.DoodleFragments[0].Strokes[0].Data[1];
        ys[0].Should().Be(0.2);
        ys[1].Should().Be(0.8);
    }

    [Fact]
    public void Apply_ZeroMapsToOne()
    {
        var comp = TestCompositionFactory.CreateSimple([0.0], [0.5]);

        var result = _transform.Apply(comp, _rng);

        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(1.0);
    }

    [Fact]
    public void Apply_OneMapsToZero()
    {
        var comp = TestCompositionFactory.CreateSimple([1.0], [0.5]);

        var result = _transform.Apply(comp, _rng);

        result.DoodleFragments[0].Strokes[0].Data[0][0].Should().Be(0.0);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        _transform.Name.Should().Be("horizontal-mirror");
    }
}
