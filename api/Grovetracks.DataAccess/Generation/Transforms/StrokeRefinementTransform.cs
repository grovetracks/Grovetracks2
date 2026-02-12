using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class StrokeRefinementTransform(double multiplier = 3.0, int minPoints = 10) : IStrokeTransform
{
    public string Name => "stroke-refinement";

    public Composition Apply(Composition source, Random rng)
    {
        var fragments = source.DoodleFragments
            .Select(fragment => new DoodleFragment
            {
                Strokes = fragment.Strokes
                    .Select(RefineStroke)
                    .ToList()
                    .AsReadOnly()
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

    private Stroke RefineStroke(Stroke stroke)
    {
        var xs = stroke.Data[0];
        var ys = stroke.Data[1];

        if (xs.Count < 2)
            return stroke;

        var cumulativeLengths = ComputeCumulativeArcLengths(xs, ys);
        var totalLength = cumulativeLengths[^1];

        if (totalLength <= 0)
            return stroke;

        var targetCount = Math.Max((int)(xs.Count * multiplier), minPoints);
        var refinedXs = new List<double>(targetCount);
        var refinedYs = new List<double>(targetCount);

        for (var i = 0; i < targetCount; i++)
        {
            var targetDistance = totalLength * i / (targetCount - 1);
            var (x, y) = InterpolateAtDistance(xs, ys, cumulativeLengths, targetDistance);

            refinedXs.Add(CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(x)));
            refinedYs.Add(CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(y)));
        }

        var timeData = stroke.Data.Count > 2 ? stroke.Data[2] : (IReadOnlyList<double>)[0];

        return new Stroke
        {
            Data = new List<IReadOnlyList<double>>
            {
                refinedXs.AsReadOnly(),
                refinedYs.AsReadOnly(),
                timeData
            }.AsReadOnly()
        };
    }

    private static List<double> ComputeCumulativeArcLengths(
        IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        var lengths = new List<double>(xs.Count) { 0.0 };

        for (var i = 1; i < xs.Count; i++)
        {
            var dx = xs[i] - xs[i - 1];
            var dy = ys[i] - ys[i - 1];
            lengths.Add(lengths[i - 1] + Math.Sqrt(dx * dx + dy * dy));
        }

        return lengths;
    }

    private static (double X, double Y) InterpolateAtDistance(
        IReadOnlyList<double> xs, IReadOnlyList<double> ys,
        List<double> cumulativeLengths, double targetDistance)
    {
        for (var i = 1; i < cumulativeLengths.Count; i++)
        {
            if (cumulativeLengths[i] >= targetDistance)
            {
                var segmentLength = cumulativeLengths[i] - cumulativeLengths[i - 1];
                if (segmentLength <= 0)
                    return (xs[i], ys[i]);

                var t = (targetDistance - cumulativeLengths[i - 1]) / segmentLength;
                var x = xs[i - 1] + t * (xs[i] - xs[i - 1]);
                var y = ys[i - 1] + t * (ys[i] - ys[i - 1]);
                return (x, y);
            }
        }

        return (xs[^1], ys[^1]);
    }
}
