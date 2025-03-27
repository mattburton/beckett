using System.Text.Json;
using Beckett.Messages;
using Beckett.MessageStorage;

namespace Beckett;

public interface IMessageContext
{
    /// <summary>
    /// The unique identifier for the message in the message store
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The name of the stream the message is contained within
    /// </summary>
    string StreamName { get; }

    /// <summary>
    /// The position of the message within the stream
    /// </summary>
    long StreamPosition { get; }

    /// <summary>
    /// The position of the message within the message store
    /// </summary>
    long GlobalPosition { get; }

    /// <summary>
    /// The message type
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Message data
    /// </summary>
    JsonElement Data { get; }

    /// <summary>
    /// Message metadata
    /// </summary>
    JsonElement Metadata { get; }

    /// <summary>
    /// The timestamp of when the message was written to the message store
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The .NET type of the message based on the mapped message types, or null if the type is not mapped. This property
    /// is lazy loaded in order to defer type mapping until requested.
    /// </summary>
    Type? MessageType { get; }

    /// <summary>
    /// The message data deserialized as an instance of the resolved .NET type, or null if the type is not mapped. This
    /// property is lazy loaded in order to defer deserialization until requested.
    /// </summary>
    object? Message { get; }

    /// <summary>
    /// Message metadata deserialized as a dictionary. This property is lazy loaded in order to defer deserialization
    /// until requested.
    /// </summary>
    Dictionary<string, string> MessageMetadata { get; }
}

public interface IMessageContext<out T> : IMessageContext where T : class
{
    /// <summary>
    /// The message data deserialized as an instance of the resolved .NET type, or null if the type is not mapped. This
    /// property is lazy loaded in order to defer deserialization until requested.
    /// </summary>
    new T? Message { get; }
}

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

public record MessageContext<T>(IMessageContext context) : IMessageContext<T> where T : class
{
    public string Id => context.Id;
    public string StreamName => context.StreamName;
    public long StreamPosition => context.StreamPosition;
    public long GlobalPosition => context.GlobalPosition;
    public string Type => context.Type;
    public JsonElement Data => context.Data;
    public JsonElement Metadata => context.Metadata;
    public DateTimeOffset Timestamp => context.Timestamp;
    public Type? MessageType => context.MessageType;
    public T? Message => context.Message as T;
    public Dictionary<string, string> MessageMetadata => context.MessageMetadata;

    object? IMessageContext.Message => Message;
}
