using System.Text.Json;
using Beckett.Messages;

namespace Beckett;

public record StreamMessage(
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
    private readonly Lazy<Dictionary<string, string>> _messageMetadata = new(Metadata.ToMetadataDictionary);

    public Type? MessageType => _messageType.Value;

    public object? Message => _message.Value;

    public Dictionary<string, string> MessageMetadata => _messageMetadata.Value;

    public IMessageContext ToMessageContext() => new MessageContext(
        Id,
        StreamName,
        StreamPosition,
        GlobalPosition,
        Type,
        Data,
        Metadata,
        Timestamp
    );
}
