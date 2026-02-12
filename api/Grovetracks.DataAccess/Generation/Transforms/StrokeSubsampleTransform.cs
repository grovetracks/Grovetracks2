using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class StrokeSubsampleTransform(int minStrokesRequired = 5) : IStrokeTransform
{
    public string Name => "stroke-subsample";

    public Composition Apply(Composition source, Random rng)
    {
        var fragments = source.DoodleFragments
            .Select(fragment =>
            {
                if (fragment.Strokes.Count < minStrokesRequired)
                    return fragment;

                var strokesToRemove = rng.Next(1, 3);
                var indices = Enumerable.Range(0, fragment.Strokes.Count)
                    .OrderBy(_ => rng.Next())
                    .Take(strokesToRemove)
                    .ToHashSet();

                var remaining = fragment.Strokes
                    .Where((_, i) => !indices.Contains(i))
                    .ToList()
                    .AsReadOnly();

                return new DoodleFragment { Strokes = remaining };
            })
            .ToList()
            .AsReadOnly();

        return new Composition
        {
            Width = source.Width,
            Height = source.Height,
            DoodleFragments = fragments,
            Tags = source.Tags
        };
    }
}
