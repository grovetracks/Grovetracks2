namespace Grovetracks.Api.Models;

public class DoodleSummaryResponse
{
    public required string KeyId { get; init; }
    public required string Word { get; init; }
    public required string CountryCode { get; init; }
    public required DateTime Timestamp { get; init; }
    public required bool Recognized { get; init; }
}

public class CompositionResponse
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required IReadOnlyList<DoodleFragmentResponse> DoodleFragments { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
}

public class DoodleFragmentResponse
{
    public required IReadOnlyList<StrokeResponse> Strokes { get; init; }
}

public class StrokeResponse
{
    public required IReadOnlyList<IReadOnlyList<double>> Data { get; init; }
}

public class DoodleWithCompositionResponse
{
    public required DoodleSummaryResponse Doodle { get; init; }
    public required CompositionResponse Composition { get; init; }
}

public class GalleryPageResponse
{
    public required IReadOnlyList<DoodleWithCompositionResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required bool HasMore { get; init; }
}
