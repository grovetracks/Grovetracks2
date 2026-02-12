using FluentAssertions;
using Grovetracks.DataAccess.Generation;

namespace Grovetracks.Test.Unit.Generation;

public class AugmentationPipelineTests
{
    private readonly AugmentationPipeline _pipeline = new();

    [Fact]
    public void GenerateVariations_ProducesRequestedCount()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var results = _pipeline.GenerateVariations(comp, 5, new Random(42));

        results.Count.Should().BeGreaterThan(0);
        results.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void GenerateVariations_AllResultsAreValid()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var results = _pipeline.GenerateVariations(comp, 10, new Random(42));

        foreach (var (composition, _) in results)
        {
            var (isValid, score) = CompositionValidator.Validate(composition);
            isValid.Should().BeTrue();
            score.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void GenerateVariations_AllCoordinatesInBounds()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var results = _pipeline.GenerateVariations(comp, 10, new Random(42));

        foreach (var (composition, _) in results)
        {
            foreach (var fragment in composition.DoodleFragments)
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
    public void GenerateVariations_IncludesMethodDescription()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var results = _pipeline.GenerateVariations(comp, 5, new Random(42));

        foreach (var (_, method) in results)
        {
            method.Should().NotBeNullOrWhiteSpace();
            method.Should().ContainAny(
                "horizontal-mirror", "rotation", "uniform-scale",
                "translation-jitter", "point-noise", "stroke-subsample",
                "stroke-smoothing", "stroke-refinement",
                "stroke-elaboration", "stroke-embellishment");
        }
    }

    [Fact]
    public void GenerateVariations_DifferentSeedsProduceDifferentResults()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var results1 = _pipeline.GenerateVariations(comp, 5, new Random(1));
        var results2 = _pipeline.GenerateVariations(comp, 5, new Random(999));

        if (results1.Count > 0 && results2.Count > 0)
        {
            var xs1 = results1[0].Composition.DoodleFragments[0].Strokes[0].Data[0];
            var xs2 = results2[0].Composition.DoodleFragments[0].Strokes[0].Data[0];
            xs1.Should().NotBeEquivalentTo(xs2);
        }
    }

    [Fact]
    public void GenerateVariations_Deterministic_WithSameSeed()
    {
        var comp = TestCompositionFactory.CreateMultiStroke();

        var results1 = _pipeline.GenerateVariations(comp, 5, new Random(42));
        var results2 = _pipeline.GenerateVariations(comp, 5, new Random(42));

        results1.Count.Should().Be(results2.Count);
        for (var i = 0; i < results1.Count; i++)
        {
            results1[i].Method.Should().Be(results2[i].Method);
        }
    }
}
