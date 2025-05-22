namespace Beckett.MessageStorage;

public class ReadIndexBatchOptions
{
    public required long StartingGlobalPosition { get; init; }
    public required int BatchSize { get; init; }
    public string? Category { get; init; }
    public string[]? Types { get; init; }
}
