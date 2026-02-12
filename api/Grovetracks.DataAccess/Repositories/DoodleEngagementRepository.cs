using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grovetracks.DataAccess.Repositories;

public class DoodleEngagementRepository(AppDbContext dbContext) : IDoodleEngagementRepository
{
    public async Task<DoodleEngagement> UpsertAsync(
        DoodleEngagement engagement,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.DoodleEngagements
            .FindAsync([engagement.KeyId], cancellationToken);

        if (existing is null)
        {
            dbContext.DoodleEngagements.Add(engagement);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(engagement);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return engagement;
    }

    public async Task<IReadOnlySet<string>> GetEngagedKeyIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var keyIds = await dbContext.DoodleEngagements
            .Select(e => e.KeyId)
            .ToListAsync(cancellationToken);

        return keyIds.ToHashSet();
    }
}
