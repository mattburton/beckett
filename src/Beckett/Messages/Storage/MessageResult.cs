namespace Beckett.Messages.Storage;

public readonly struct MessageResult(
    string id,
    string streamName,
    long streamPosition,
    long globalPosition,
    Type type,
    object message,
    IDictionary<string, object> metadata,
    DateTimeOffset timestamp
)
{
    public string Id { get; } = id;
    public string StreamName { get; } = streamName;
    public long StreamPosition { get; } = streamPosition;
    public long GlobalPosition { get; } = globalPosition;
    public Type Type { get; } = type;
    public object Message { get; } = message;
    public IDictionary<string, object> Metadata { get; } = metadata;
    public DateTimeOffset Timestamp { get; } = timestamp;
}
