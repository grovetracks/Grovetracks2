namespace Grovetracks.Api.Models;

public class SeedCompositionSummaryResponse
{
    public required Guid Id { get; init; }
    public required string Word { get; init; }
    public required double QualityScore { get; init; }
    public required int StrokeCount { get; init; }
    public required int TotalPointCount { get; init; }
    public required DateTime CuratedAt { get; init; }
    public required string SourceType { get; init; }
    public string? GenerationMethod { get; init; }
}

public class SeedCompositionWithDataResponse
{
    public required SeedCompositionSummaryResponse Summary { get; init; }
    public required CompositionResponse Composition { get; init; }
}

public class SeedCompositionPageResponse
{
    public required IReadOnlyList<SeedCompositionWithDataResponse> Items { get; init; }
    public required int TotalCount { get; init; }
}
