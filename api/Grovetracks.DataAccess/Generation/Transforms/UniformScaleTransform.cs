using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class UniformScaleTransform(double minScale = 0.6, double maxScale = 0.9) : IStrokeTransform
{
    public string Name => "uniform-scale";

    public Composition Apply(Composition source, Random rng)
    {
        var scale = minScale + rng.NextDouble() * (maxScale - minScale);
        var (minX, minY, maxX, maxY) = CompositionGeometry.ComputeBoundingBox(source);

        var bboxWidth = maxX - minX;
        var bboxHeight = maxY - minY;
        if (bboxWidth <= 0 || bboxHeight <= 0)
            return source;

        var centerX = (minX + maxX) / 2.0;
        var centerY = (minY + maxY) / 2.0;

        return CompositionGeometry.TransformComposition(source, (x, y) =>
        {
            var nx = CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate((x - centerX) * scale + centerX));
            var ny = CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate((y - centerY) * scale + centerY));
            return (nx, ny);
        });
    }
}
