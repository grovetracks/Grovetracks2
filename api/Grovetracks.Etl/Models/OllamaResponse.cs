namespace Grovetracks.Etl.Models;

public class OllamaChatResponse
{
    public required OllamaChatMessage Message { get; init; }
    public bool Done { get; init; }
}

public class OllamaChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}
