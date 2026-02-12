namespace Grovetracks.DataAccess.Generation;

public class SceneSlot
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public double FillFactor { get; init; } = 0.75;
}

public class SceneTemplate
{
    public required string Name { get; init; }
    public required IReadOnlyList<SceneSlot> Slots { get; init; }
}
