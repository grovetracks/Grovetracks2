using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class PointNoiseTransform(double sigma = 0.005) : IStrokeTransform
{
    public string Name => "point-noise";

    public Composition Apply(Composition source, Random rng)
    {
        return CompositionGeometry.TransformComposition(source, (x, y) =>
        {
            var nx = CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(x + GaussianNoise(rng)));
            var ny = CompositionGeometry.RoundCoordinate(
                CompositionGeometry.ClampCoordinate(y + GaussianNoise(rng)));
            return (nx, ny);
        });
    }

    private double GaussianNoise(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        var stdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return stdNormal * sigma;
    }
}
