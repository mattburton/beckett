using System.Text.Json;
using Beckett.MessageStorage;

namespace Beckett.Messages;

public record MessageContext(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string Type,
    JsonElement Data,
    JsonElement Metadata,
    DateTimeOffset Timestamp,
    Type? MessageType = null,
    object? Message = null,
    Dictionary<string, string>? MessageMetadata = null
) : IMessageContext
{
    private readonly Lazy<Type?> _messageType = new(() => MessageType ?? (MessageTypeMap.TryGetType(Type, out var type) ? type : null));
    private readonly Lazy<object?> _message = new(() => Message ?? MessageSerializer.Deserialize(Type, Data));
    private readonly Lazy<Dictionary<string, string>> _messageMetadata = new(MessageMetadata ?? Metadata.ToMetadataDictionary());

    public Type? MessageType => _messageType.Value;

    public object? Message => _message.Value;

    public Dictionary<string, string> MessageMetadata => _messageMetadata.Value;

    public static IMessageContext From(StreamMessage streamMessage) => new MessageContext(
        streamMessage.Id,
        streamMessage.StreamName,
        streamMessage.StreamPosition,
        streamMessage.GlobalPosition,
        streamMessage.Type,
        streamMessage.Data,
        streamMessage.Metadata,
        streamMessage.Timestamp
    );

    public static MessageContext From(object message) => new(
        string.Empty,
        string.Empty,
        0,
        0,
        string.Empty,
        EmptyJsonElement.Instance,
        EmptyJsonElement.Instance,
        DateTimeOffset.UtcNow,
        message.GetType(),
        message
    );
}
