using FluentAssertions;
using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.Test.Unit.Generation;

public class CompositionValidatorTests
{
    [Fact]
    public void Validate_ValidComposition_ReturnsTrue()
    {
        var comp = TestCompositionFactory.CreateMultiStroke(strokeCount: 7, pointsPerStroke: 12);

        var (isValid, score) = CompositionValidator.Validate(comp);

        isValid.Should().BeTrue();
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Validate_EmptyFragments_ReturnsFalse()
    {
        var comp = new Composition
        {
            Width = 255,
            Height = 255,
            DoodleFragments = new List<DoodleFragment>().AsReadOnly(),
            Tags = new List<string> { "test" }.AsReadOnly()
        };

        var (isValid, _) = CompositionValidator.Validate(comp);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TooFewPoints_ReturnsFalse()
    {
        var comp = TestCompositionFactory.CreateSimple([0.5], [0.5]);

        var (isValid, _) = CompositionValidator.Validate(comp);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_OutOfBoundsCoordinates_ReturnsFalse()
    {
        var stroke = new Stroke
        {
            Data = new List<IReadOnlyList<double>>
            {
                new List<double> { -0.1, 0.5, 1.1, 0.3, 0.4, 0.5 }.AsReadOnly(),
                new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 }.AsReadOnly(),
                new List<double> { 0 }.AsReadOnly()
            }.AsReadOnly()
        };

        var comp = new Composition
        {
            Width = 255,
            Height = 255,
            DoodleFragments = new List<DoodleFragment>
            {
                new() { Strokes = new List<Stroke> { stroke }.AsReadOnly() }
            }.AsReadOnly(),
            Tags = new List<string> { "test" }.AsReadOnly()
        };

        var (isValid, _) = CompositionValidator.Validate(comp);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TinyBoundingBox_ReturnsFalse()
    {
        var comp = TestCompositionFactory.CreateSimple(
            [0.5, 0.51, 0.5, 0.51, 0.5, 0.51],
            [0.5, 0.51, 0.5, 0.51, 0.5, 0.51]);

        var (isValid, _) = CompositionValidator.Validate(comp);

        isValid.Should().BeFalse();
    }
}
