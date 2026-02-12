namespace Grovetracks.DataAccess.Models;

public class RawDrawing
{
    public required IReadOnlyList<RawStroke> Strokes { get; init; }
}

public class RawStroke
{
    public required IReadOnlyList<double> Xs { get; init; }
    public required IReadOnlyList<double> Ys { get; init; }
}
