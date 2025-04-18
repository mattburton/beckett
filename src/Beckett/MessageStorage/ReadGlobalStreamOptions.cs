namespace Beckett.MessageStorage;

public class ReadGlobalStreamOptions
{
    public required long StartingGlobalPosition { get; init; }
    public required int Count { get; init; }
    public string? Category { get; init; }
    public string[]? Types { get; init; }
}
