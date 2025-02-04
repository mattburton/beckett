using System.Text.Json;

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
