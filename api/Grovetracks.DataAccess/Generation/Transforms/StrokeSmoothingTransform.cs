using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class StrokeSmoothingTransform(int resolution = 3) : IStrokeTransform
{
    public string Name => "stroke-smoothing";

    public Composition Apply(Composition source, Random rng)
    {
        var fragments = source.DoodleFragments
            .Select(fragment => new DoodleFragment
            {
                Strokes = fragment.Strokes
                    .Select(SmoothStroke)
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

    private Stroke SmoothStroke(Stroke stroke)
    {
        var xs = stroke.Data[0];
        var ys = stroke.Data[1];

        if (xs.Count < 3)
            return stroke;

        var smoothXs = new List<double>();
        var smoothYs = new List<double>();

        for (var i = 0; i < xs.Count - 1; i++)
        {
            var p0X = xs[Math.Max(i - 1, 0)];
            var p0Y = ys[Math.Max(i - 1, 0)];
            var p1X = xs[i];
            var p1Y = ys[i];
            var p2X = xs[Math.Min(i + 1, xs.Count - 1)];
            var p2Y = ys[Math.Min(i + 1, ys.Count - 1)];
            var p3X = xs[Math.Min(i + 2, xs.Count - 1)];
            var p3Y = ys[Math.Min(i + 2, ys.Count - 1)];

            smoothXs.Add(CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(p1X)));
            smoothYs.Add(CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(p1Y)));

            for (var j = 1; j <= resolution; j++)
            {
                var t = j / (double)(resolution + 1);
                var x = CatmullRom(p0X, p1X, p2X, p3X, t);
                var y = CatmullRom(p0Y, p1Y, p2Y, p3Y, t);

                smoothXs.Add(CompositionGeometry.RoundCoordinate(
                    CompositionGeometry.ClampCoordinate(x)));
                smoothYs.Add(CompositionGeometry.RoundCoordinate(
                    CompositionGeometry.ClampCoordinate(y)));
            }
        }

        smoothXs.Add(CompositionGeometry.RoundCoordinate(
            CompositionGeometry.ClampCoordinate(xs[^1])));
        smoothYs.Add(CompositionGeometry.RoundCoordinate(
            CompositionGeometry.ClampCoordinate(ys[^1])));

        var timeData = stroke.Data.Count > 2 ? stroke.Data[2] : (IReadOnlyList<double>)[0];

        return new Stroke
        {
            Data = new List<IReadOnlyList<double>>
            {
                smoothXs.AsReadOnly(),
                smoothYs.AsReadOnly(),
                timeData
            }.AsReadOnly()
        };
    }

    private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5 * (
            (2.0 * p1) +
            (-p0 + p2) * t +
            (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
            (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3);
    }
}
