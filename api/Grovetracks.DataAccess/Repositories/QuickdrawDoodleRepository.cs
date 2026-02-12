using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grovetracks.DataAccess.Repositories;

public class QuickdrawDoodleRepository(AppDbContext dbContext) : IQuickdrawDoodleRepository
{
    public async Task<IReadOnlyList<QuickdrawDoodle>> GetByWordAsync(
        string word,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawDoodles
            .Where(d => d.Word == word)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuickdrawDoodle>> GetByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawDoodles
            .Where(d => d.Word == word)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<QuickdrawDoodle?> GetByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawDoodles
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.KeyId == keyId, cancellationToken);
    }

    public async Task<IReadOnlyList<QuickdrawDoodle>> GetByWordExcludingKeysAsync(
        string word,
        int limit,
        IReadOnlySet<string> excludedKeyIds,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawDoodles
            .Where(d => d.Word == word && !excludedKeyIds.Contains(d.KeyId))
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawDoodles
            .Select(d => d.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(cancellationToken);
    }
}
