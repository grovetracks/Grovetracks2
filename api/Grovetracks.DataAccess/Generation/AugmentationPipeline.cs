using Grovetracks.DataAccess.Generation.Transforms;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation;

public class AugmentationPipeline
{
    private readonly IReadOnlyList<IStrokeTransform> _transforms;

    public AugmentationPipeline(IReadOnlyList<IStrokeTransform>? transforms = null)
    {
        _transforms = transforms ??
        [
            new HorizontalMirrorTransform(),
            new RotationTransform(),
            new UniformScaleTransform(),
            new TranslationJitterTransform(),
            new PointNoiseTransform(),
            new StrokeSubsampleTransform(),
            new StrokeSmoothingTransform(),
            new StrokeRefinementTransform(),
            new StrokeElaborationTransform(),
            new StrokeEmbellishmentTransform()
        ];
    }

    public IReadOnlyList<(Composition Composition, string Method)> GenerateVariations(
        Composition source,
        int count,
        Random rng)
    {
        var results = new List<(Composition, string)>(count);

        for (var i = 0; i < count; i++)
        {
            var numTransforms = rng.Next(1, 4);
            var selectedTransforms = _transforms
                .OrderBy(_ => rng.Next())
                .Take(numTransforms)
                .ToList();

            var current = source;
            var methodNames = new List<string>(numTransforms);

            foreach (var transform in selectedTransforms)
            {
                current = transform.Apply(current, rng);
                methodNames.Add(transform.Name);
            }

            var (isValid, _) = CompositionValidator.Validate(current);
            if (isValid)
            {
                var method = string.Join("+", methodNames);
                results.Add((current, method));
            }
        }

        return results.AsReadOnly();
    }
}
