using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class TranslationJitterTransform(double maxOffset = 0.15) : IStrokeTransform
{
    public string Name => "translation-jitter";

    public Composition Apply(Composition source, Random rng)
    {
        var (minX, minY, maxX, maxY) = CompositionGeometry.ComputeBoundingBox(source);

        var safeMinDx = -minX;
        var safeMaxDx = 1.0 - maxX;
        var safeMinDy = -minY;
        var safeMaxDy = 1.0 - maxY;

        var dx = Math.Clamp(
            (rng.NextDouble() * 2.0 - 1.0) * maxOffset,
            safeMinDx, safeMaxDx);
        var dy = Math.Clamp(
            (rng.NextDouble() * 2.0 - 1.0) * maxOffset,
            safeMinDy, safeMaxDy);

        return CompositionGeometry.TransformComposition(source, (x, y) =>
        {
            var nx = CompositionGeometry.RoundCoordinate(CompositionGeometry.ClampCoordinate(x + dx));
            var ny = CompositionGeometry.RoundCoordinate(CompositionGeometry.ClampCoordinate(y + dy));
            return (nx, ny);
        });
    }
}
