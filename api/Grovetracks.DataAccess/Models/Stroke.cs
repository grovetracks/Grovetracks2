namespace Grovetracks.DataAccess.Models;

public class Stroke
{
    public required IReadOnlyList<IReadOnlyList<double>> Data { get; init; }
}
