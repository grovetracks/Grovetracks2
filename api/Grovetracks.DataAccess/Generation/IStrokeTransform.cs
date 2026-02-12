using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation;

public interface IStrokeTransform
{
    string Name { get; }
    Composition Apply(Composition source, Random rng);
}
