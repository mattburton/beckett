namespace Beckett.Storage;

public class ReadIndexBatchOptions
{
    public required long StartingGlobalPosition { get; init; }
    public required int BatchSize { get; init; }
}
