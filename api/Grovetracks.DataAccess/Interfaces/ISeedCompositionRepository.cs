using Grovetracks.DataAccess.Entities;

namespace Grovetracks.DataAccess.Interfaces;

public interface ISeedCompositionRepository
{
    Task<IReadOnlyList<SeedComposition>> GetByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeedComposition>> GetByWordAsync(
        string word,
        string? sourceType,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeedComposition>> GetByWordAsync(
        string word,
        string? sourceType,
        string? generationMethod,
        int limit,
        CancellationToken cancellationToken = default);

    Task<SeedComposition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        string? sourceType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        string? sourceType,
        string? generationMethod,
        CancellationToken cancellationToken = default);

    Task<int> GetCountByWordAsync(
        string word,
        CancellationToken cancellationToken = default);

    Task<int> GetCountByWordAsync(
        string word,
        string? sourceType,
        CancellationToken cancellationToken = default);

    Task<int> GetCountByWordAsync(
        string word,
        string? sourceType,
        string? generationMethod,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyList<SeedComposition> compositions,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalCountAsync(
        CancellationToken cancellationToken = default);

    Task<int> GetTotalCountAsync(
        string? sourceType,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalCountAsync(
        string? sourceType,
        string? generationMethod,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctGenerationMethodsAsync(
        string? sourceType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeedComposition>> GetCuratedByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> DeleteBySourceTypeAsync(
        string sourceType,
        CancellationToken cancellationToken = default);
}
