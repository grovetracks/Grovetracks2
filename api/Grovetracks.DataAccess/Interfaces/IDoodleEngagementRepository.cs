using Grovetracks.DataAccess.Entities;

namespace Grovetracks.DataAccess.Interfaces;

public interface IDoodleEngagementRepository
{
    Task<DoodleEngagement> UpsertAsync(
        DoodleEngagement engagement,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetEngagedKeyIdsAsync(
        CancellationToken cancellationToken = default);
}
