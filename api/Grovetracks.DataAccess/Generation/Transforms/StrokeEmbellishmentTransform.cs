using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class StrokeEmbellishmentTransform : IStrokeTransform
{
    private enum EmbellishmentMode { Shadow, Echo, Connectors }

    public string Name => "stroke-embellishment";

    public Composition Apply(Composition source, Random rng)
    {
        var mode = (EmbellishmentMode)rng.Next(3);

        var fragments = source.DoodleFragments
            .Select(fragment => mode switch
            {
                EmbellishmentMode.Shadow => ApplyShadow(fragment, rng),
                EmbellishmentMode.Echo => ApplyEcho(fragment, rng),
                EmbellishmentMode.Connectors => ApplyConnectors(fragment),
                _ => fragment
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

    private static DoodleFragment ApplyShadow(DoodleFragment fragment, Random rng)
    {
        var dx = 0.01 + rng.NextDouble() * 0.01;
        var dy = 0.01 + rng.NextDouble() * 0.01;

        var shadowStrokes = new List<Stroke>();
        foreach (var stroke in fragment.Strokes)
        {
            var xs = stroke.Data[0];
            var ys = stroke.Data[1];

            var shadowXs = new List<double>(xs.Count);
            var shadowYs = new List<double>(ys.Count);

            for (var i = 0; i < xs.Count; i++)
            {
                shadowXs.Add(CompositionGeometry.RoundCoordinate(
                    CompositionGeometry.ClampCoordinate(xs[i] + dx)));
                shadowYs.Add(CompositionGeometry.RoundCoordinate(
                    CompositionGeometry.ClampCoordinate(ys[i] + dy)));
            }

            var timeData = stroke.Data.Count > 2 ? stroke.Data[2] : (IReadOnlyList<double>)[0];

            shadowStrokes.Add(new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    shadowXs.AsReadOnly(),
                    shadowYs.AsReadOnly(),
                    timeData
                }.AsReadOnly()
            });
        }

        var allStrokes = new List<Stroke>(shadowStrokes.Count + fragment.Strokes.Count);
        allStrokes.AddRange(shadowStrokes);
        allStrokes.AddRange(fragment.Strokes);

        return new DoodleFragment { Strokes = allStrokes.AsReadOnly() };
    }

    private static DoodleFragment ApplyEcho(DoodleFragment fragment, Random rng)
    {
        var scaleFactor = 0.93 + rng.NextDouble() * 0.04;
        var allStrokes = new List<Stroke>(fragment.Strokes.Count * 2);

        foreach (var stroke in fragment.Strokes)
        {
            allStrokes.Add(stroke);

            var xs = stroke.Data[0];
            var ys = stroke.Data[1];

            if (xs.Count < 2)
                continue;

            var centroidX = xs.Average();
            var centroidY = ys.Average();

            var echoXs = new List<double>(xs.Count);
            var echoYs = new List<double>(ys.Count);

            for (var i = 0; i < xs.Count; i++)
            {
                var ex = centroidX + (xs[i] - centroidX) * scaleFactor;
                var ey = centroidY + (ys[i] - centroidY) * scaleFactor;
                echoXs.Add(CompositionGeometry.RoundCoordinate(
                    CompositionGeometry.ClampCoordinate(ex)));
                echoYs.Add(CompositionGeometry.RoundCoordinate(
                    CompositionGeometry.ClampCoordinate(ey)));
            }

            var timeData = stroke.Data.Count > 2 ? stroke.Data[2] : (IReadOnlyList<double>)[0];

            allStrokes.Add(new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    echoXs.AsReadOnly(),
                    echoYs.AsReadOnly(),
                    timeData
                }.AsReadOnly()
            });
        }

        return new DoodleFragment { Strokes = allStrokes.AsReadOnly() };
    }

    private static DoodleFragment ApplyConnectors(DoodleFragment fragment)
    {
        if (fragment.Strokes.Count < 2)
            return fragment;

        var allStrokes = new List<Stroke>(fragment.Strokes);

        for (var i = 0; i < fragment.Strokes.Count - 1; i++)
        {
            var currentStroke = fragment.Strokes[i];
            var nextStroke = fragment.Strokes[i + 1];

            var endX = currentStroke.Data[0][^1];
            var endY = currentStroke.Data[1][^1];
            var startX = nextStroke.Data[0][0];
            var startY = nextStroke.Data[1][0];

            var connectorXs = new List<double> { endX, startX };
            var connectorYs = new List<double> { endY, startY };

            allStrokes.Add(new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    connectorXs.AsReadOnly(),
                    connectorYs.AsReadOnly(),
                    new List<double> { 0 }.AsReadOnly()
                }.AsReadOnly()
            });
        }

        return new DoodleFragment { Strokes = allStrokes.AsReadOnly() };
    }
}
