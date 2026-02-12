using Grovetracks.DataAccess.Entities;

namespace Grovetracks.DataAccess.Interfaces;

public interface IQuickdrawSimpleDoodleRepository
{
    Task<IReadOnlyList<QuickdrawSimpleDoodle>> GetByWordAsync(
        string word,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuickdrawSimpleDoodle>> GetByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default);

    Task<QuickdrawSimpleDoodle?> GetByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        CancellationToken cancellationToken = default);

    Task<int> GetCountByWordAsync(
        string word,
        CancellationToken cancellationToken = default);
}
