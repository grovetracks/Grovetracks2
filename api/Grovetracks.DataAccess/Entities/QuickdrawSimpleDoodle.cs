namespace Grovetracks.DataAccess.Entities;

public class QuickdrawSimpleDoodle
{
    public required string KeyId { get; init; }
    public required string Word { get; init; }
    public required string CountryCode { get; init; }
    public required DateTime Timestamp { get; init; }
    public required bool Recognized { get; init; }
    public required string Drawing { get; init; }
}
