namespace Grovetracks.Etl.Models;

public class AiCompositionBatch
{
    public required IReadOnlyList<AiComposition> Compositions { get; init; }
}

public class AiComposition
{
    public required string Subject { get; init; }
    public required IReadOnlyList<AiStroke> Strokes { get; init; }
}

public class AiStroke
{
    public required IReadOnlyList<double> Xs { get; init; }
    public required IReadOnlyList<double> Ys { get; init; }
}
