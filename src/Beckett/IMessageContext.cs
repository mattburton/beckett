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
    JsonDocument Data { get; }

    /// <summary>
    /// Message metadata
    /// </summary>
    JsonDocument Metadata { get; }

    /// <summary>
    /// The timestamp of when the message was written to the message store
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The .NET type of the message based on the mapped message types, or null if the type is not mapped
    /// </summary>
    Lazy<Type?> ResolvedType { get; }

    /// <summary>
    /// The message data deserialized as an instance of the resolved .NET type, or null if the type is not mapped
    /// </summary>
    Lazy<object?> ResolvedMessage { get; }

    /// <summary>
    /// The message metadata deserialized as a <see cref="Dictionary{String,Object}"/>
    /// </summary>
    Lazy<Dictionary<string, object>?> ResolvedMetadata { get; }

    /// <summary>
    /// Service provider that can be used to resolve services within a handler. For a static handler function this will
    /// be the root service provider, but otherwise this will be scoped to the handler execution.
    /// </summary>
    IServiceProvider Services { get; }
}
