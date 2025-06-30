namespace Beckett.Storage;

public class ReadGlobalStreamOptions
{
    public required long LastGlobalPosition { get; init; }
    public required int BatchSize { get; init; }
}
