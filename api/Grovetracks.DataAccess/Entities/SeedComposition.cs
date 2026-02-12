namespace Grovetracks.DataAccess.Entities;

public class SeedComposition
{
    public required Guid Id { get; init; }
    public required string Word { get; init; }
    public required string SourceKeyId { get; init; }
    public required double QualityScore { get; init; }
    public required int StrokeCount { get; init; }
    public required int TotalPointCount { get; init; }
    public required string CompositionJson { get; init; }
    public required DateTime CuratedAt { get; init; }
    public string SourceType { get; init; } = "curated";
    public string? GenerationMethod { get; init; }
    public string? SourceCompositionIds { get; init; }
}
