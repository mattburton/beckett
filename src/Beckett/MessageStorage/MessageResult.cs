using System.Text.Json;
using Beckett.Messages;

namespace Beckett.MessageStorage;

public readonly struct MessageResult(
    string id,
    string streamName,
    long streamPosition,
    long globalPosition,
    string type,
    JsonDocument data,
    IDictionary<string, object> metadata,
    DateTimeOffset timestamp
)
{
    public string Id { get; } = id;
    public string StreamName { get; } = streamName;
    public long StreamPosition { get; } = streamPosition;
    public long GlobalPosition { get; } = globalPosition;
    public string Type { get; } = type;
    public JsonDocument Data { get; } = data;
    public IDictionary<string, object> Metadata { get; } = metadata;
    public DateTimeOffset Timestamp { get; } = timestamp;

    public Lazy<Type?> ResolvedType { get; } =
        new(MessageTypeMap.TryGetType(type, out var resolvedType) ? resolvedType : null);

    public Lazy<object?> ResolvedMessage { get; } = new(StaticMessageSerializer.Deserialize(type, data));
}
