using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grovetracks.DataAccess.Repositories;

public class QuickdrawSimpleDoodleRepository(AppDbContext dbContext) : IQuickdrawSimpleDoodleRepository
{
    public async Task<IReadOnlyList<QuickdrawSimpleDoodle>> GetByWordAsync(
        string word,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawSimpleDoodles
            .Where(d => d.Word == word)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuickdrawSimpleDoodle>> GetByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawSimpleDoodles
            .Where(d => d.Word == word)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<QuickdrawSimpleDoodle?> GetByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawSimpleDoodles
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.KeyId == keyId, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawSimpleDoodles
            .Select(d => d.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByWordAsync(
        string word,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.QuickdrawSimpleDoodles
            .Where(d => d.Word == word)
            .CountAsync(cancellationToken);
    }
}
