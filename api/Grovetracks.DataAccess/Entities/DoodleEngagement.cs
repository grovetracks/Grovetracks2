namespace Grovetracks.DataAccess.Entities;

public class DoodleEngagement
{
    public required string KeyId { get; init; }
    public required double Score { get; init; }
    public required DateTime EngagedAt { get; init; }
}
