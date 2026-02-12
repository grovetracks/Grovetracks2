namespace Grovetracks.Api.Models;

public class CreateEngagementRequest
{
    public required string KeyId { get; init; }
    public required double Score { get; init; }
}

public class EngagementResponse
{
    public required string KeyId { get; init; }
    public required double Score { get; init; }
    public required DateTime EngagedAt { get; init; }
}
