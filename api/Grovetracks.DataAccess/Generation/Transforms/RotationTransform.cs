using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class RotationTransform(double maxDegrees = 15.0) : IStrokeTransform
{
    public string Name => "rotation";

    public Composition Apply(Composition source, Random rng)
    {
        var degrees = (rng.NextDouble() * 2.0 - 1.0) * maxDegrees;
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        return CompositionGeometry.TransformComposition(source, (x, y) =>
        {
            var cx = x - 0.5;
            var cy = y - 0.5;
            var nx = CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(cx * cos - cy * sin + 0.5));
            var ny = CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(cx * sin + cy * cos + 0.5));
            return (nx, ny);
        });
    }
}
