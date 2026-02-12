namespace Grovetracks.DataAccess.Models;

public class Composition
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required IReadOnlyList<DoodleFragment> DoodleFragments { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
}
