using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation.Transforms;

public class HorizontalMirrorTransform : IStrokeTransform
{
    public string Name => "horizontal-mirror";

    public Composition Apply(Composition source, Random rng)
    {
        return CompositionGeometry.TransformComposition(source, (x, y) =>
        {
            var nx = CompositionGeometry.RoundCoordinate(1.0 - x);
            return (nx, y);
        });
    }
}
