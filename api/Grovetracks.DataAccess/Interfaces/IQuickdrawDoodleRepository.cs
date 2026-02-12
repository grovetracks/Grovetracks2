using Grovetracks.DataAccess.Entities;

namespace Grovetracks.DataAccess.Interfaces;

public interface IQuickdrawDoodleRepository
{
    Task<IReadOnlyList<QuickdrawDoodle>> GetByWordAsync(
        string word,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuickdrawDoodle>> GetByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default);

    Task<QuickdrawDoodle?> GetByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuickdrawDoodle>> GetByWordExcludingKeysAsync(
        string word,
        int limit,
        IReadOnlySet<string> excludedKeyIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        CancellationToken cancellationToken = default);
}
