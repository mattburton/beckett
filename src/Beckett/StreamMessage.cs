using System.Text.Json;
using Beckett.Messages;

namespace Beckett;

public readonly record struct StreamMessage(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string Type,
    JsonDocument Data,
    JsonDocument Metadata,
    DateTimeOffset Timestamp
)
{
    private readonly Lazy<Type?> _messageType = new(() => MessageTypeMap.TryGetType(Type, out var type) ? type : null);
    private readonly Lazy<object?> _message = new(() => MessageSerializer.Deserialize(Type, Data));
    private readonly Lazy<Dictionary<string, object>> _messageMetadata = new(Metadata.ToMetadataDictionary);

    public Type? MessageType => _messageType.Value;

    public object? Message => _message.Value;

    public Dictionary<string, object> MessageMetadata => _messageMetadata.Value;
}