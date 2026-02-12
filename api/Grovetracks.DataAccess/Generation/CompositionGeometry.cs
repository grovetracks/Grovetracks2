using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation;

public static class CompositionGeometry
{
    private const int CoordinatePrecision = 3;

    public static (double MinX, double MinY, double MaxX, double MaxY) ComputeBoundingBox(Composition composition)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var fragment in composition.DoodleFragments)
        {
            foreach (var stroke in fragment.Strokes)
            {
                var xs = stroke.Data[0];
                var ys = stroke.Data[1];

                for (var i = 0; i < xs.Count; i++)
                {
                    if (xs[i] < minX) minX = xs[i];
                    if (xs[i] > maxX) maxX = xs[i];
                    if (ys[i] < minY) minY = ys[i];
                    if (ys[i] > maxY) maxY = ys[i];
                }
            }
        }

        return (minX, minY, maxX, maxY);
    }

    public static double ClampCoordinate(double value) => Math.Clamp(value, 0.0, 1.0);

    public static double RoundCoordinate(double value) => Math.Round(value, CoordinatePrecision);

    public static Composition TransformComposition(
        Composition source,
        Func<double, double, (double X, double Y)> pointTransform,
        IReadOnlyList<string>? tags = null)
    {
        var fragments = source.DoodleFragments
            .Select(fragment => new DoodleFragment
            {
                Strokes = fragment.Strokes
                    .Select(stroke => TransformStroke(stroke, pointTransform))
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
            Tags = tags ?? source.Tags
        };
    }

    public static Composition PlaceInRegion(
        Composition source,
        double regionX,
        double regionY,
        double regionWidth,
        double regionHeight,
        double fillFactor = 0.8)
    {
        var (minX, minY, maxX, maxY) = ComputeBoundingBox(source);
        var bboxWidth = maxX - minX;
        var bboxHeight = maxY - minY;

        if (bboxWidth <= 0 || bboxHeight <= 0)
            return source;

        var scale = Math.Min(regionWidth / bboxWidth, regionHeight / bboxHeight) * fillFactor;
        var scaledWidth = bboxWidth * scale;
        var scaledHeight = bboxHeight * scale;

        var offsetX = regionX + (regionWidth - scaledWidth) / 2.0;
        var offsetY = regionY + (regionHeight - scaledHeight) / 2.0;

        return TransformComposition(source, (x, y) =>
        {
            var nx = RoundCoordinate(ClampCoordinate((x - minX) * scale + offsetX));
            var ny = RoundCoordinate(ClampCoordinate((y - minY) * scale + offsetY));
            return (nx, ny);
        });
    }

    public static int CountTotalPoints(Composition composition)
    {
        return composition.DoodleFragments
            .SelectMany(f => f.Strokes)
            .Sum(s => s.Data[0].Count);
    }

    public static int CountTotalStrokes(Composition composition)
    {
        return composition.DoodleFragments
            .Sum(f => f.Strokes.Count);
    }

    private static Stroke TransformStroke(
        Stroke stroke,
        Func<double, double, (double X, double Y)> pointTransform)
    {
        var xs = stroke.Data[0];
        var ys = stroke.Data[1];
        var newXs = new List<double>(xs.Count);
        var newYs = new List<double>(ys.Count);

        for (var i = 0; i < xs.Count; i++)
        {
            var (nx, ny) = pointTransform(xs[i], ys[i]);
            newXs.Add(nx);
            newYs.Add(ny);
        }

        var timeData = stroke.Data.Count > 2 ? stroke.Data[2] : (IReadOnlyList<double>)[0];

        return new Stroke
        {
            Data = new List<IReadOnlyList<double>>
            {
                newXs.AsReadOnly(),
                newYs.AsReadOnly(),
                timeData
            }.AsReadOnly()
        };
    }
}
