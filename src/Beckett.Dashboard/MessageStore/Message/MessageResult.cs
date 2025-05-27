namespace Beckett.Dashboard.MessageStore.Message;

public record MessageResult(
    string Id,
    string Category,
    string StreamName,
    long GlobalPosition,
    long StreamPosition,
    long StreamVersion,
    string Type,
    DateTimeOffset Timestamp,
    string Data,
    Dictionary<string, string> Metadata
);
