using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class StrokeElaborationTransform(
    double minOffset = 0.008,
    double maxOffset = 0.025) : IStrokeTransform
{
    public string Name => "stroke-elaboration";

    public Composition Apply(Composition source, Random rng)
    {
        var fragments = source.DoodleFragments
            .Select(fragment => ElaborateFragment(fragment, rng))
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

    private DoodleFragment ElaborateFragment(DoodleFragment fragment, Random rng)
    {
        var allStrokes = new List<Stroke>();
        var offset = minOffset + rng.NextDouble() * (maxOffset - minOffset);
        var parallelCount = rng.Next(1, 3);

        foreach (var stroke in fragment.Strokes)
        {
            allStrokes.Add(stroke);

            var xs = stroke.Data[0];
            var ys = stroke.Data[1];

            if (xs.Count < 2)
                continue;

            var normals = ComputeNormals(xs, ys);

            for (var p = 0; p < parallelCount; p++)
            {
                var sign = p == 0 ? 1.0 : -1.0;
                var parallelXs = new List<double>(xs.Count);
                var parallelYs = new List<double>(ys.Count);

                for (var i = 0; i < xs.Count; i++)
                {
                    var nx = CompositionGeometry.RoundCoordinate(
                        CompositionGeometry.ClampCoordinate(xs[i] + normals[i].Nx * offset * sign));
                    var ny = CompositionGeometry.RoundCoordinate(
                        CompositionGeometry.ClampCoordinate(ys[i] + normals[i].Ny * offset * sign));
                    parallelXs.Add(nx);
                    parallelYs.Add(ny);
                }

                var timeData = stroke.Data.Count > 2 ? stroke.Data[2] : (IReadOnlyList<double>)[0];

                allStrokes.Add(new Stroke
                {
                    Data = new List<IReadOnlyList<double>>
                    {
                        parallelXs.AsReadOnly(),
                        parallelYs.AsReadOnly(),
                        timeData
                    }.AsReadOnly()
                });
            }
        }

        return new DoodleFragment { Strokes = allStrokes.AsReadOnly() };
    }

    private static List<(double Nx, double Ny)> ComputeNormals(
        IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        var normals = new List<(double, double)>(xs.Count);

        for (var i = 0; i < xs.Count; i++)
        {
            double dx, dy;

            if (i == 0)
            {
                dx = xs[1] - xs[0];
                dy = ys[1] - ys[0];
            }
            else if (i == xs.Count - 1)
            {
                dx = xs[i] - xs[i - 1];
                dy = ys[i] - ys[i - 1];
            }
            else
            {
                dx = xs[i + 1] - xs[i - 1];
                dy = ys[i + 1] - ys[i - 1];
            }

            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0)
            {
                normals.Add((0, 0));
                continue;
            }

            normals.Add((-dy / length, dx / length));
        }

        return normals;
    }
}
