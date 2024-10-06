using System.Text.Json;
using Beckett.Messages;

namespace Beckett;

public readonly struct StreamMessage(
    string id,
    string streamName,
    long streamPosition,
    long globalPosition,
    string type,
    JsonDocument data,
    JsonDocument metadata,
    DateTimeOffset timestamp
)
{
    public string Id { get; } = id;
    public string StreamName { get; } = streamName;
    public long StreamPosition { get; } = streamPosition;
    public long GlobalPosition { get; } = globalPosition;
    public string Type { get; } = type;
    public JsonDocument Data { get; } = data;
    public JsonDocument Metadata { get; } = metadata;
    public DateTimeOffset Timestamp { get; } = timestamp;

    public Lazy<Type?> ResolvedType { get; } = new(
        MessageTypeMap.TryGetType(type, out var resolvedType) ? resolvedType : null
    );

    public Lazy<object?> ResolvedMessage { get; } = new(MessageSerializer.Deserialize(type, data));

    public Lazy<Dictionary<string, object>?> ResolvedMetadata { get; } = new(metadata.ToMetadataDictionary());
}
