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
    /// The .NET type of the message as mapped from the message store
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// The message body deserialized as its corresponding .NET type
    /// </summary>
    object Message { get; }

    /// <summary>
    /// Message metadata
    /// </summary>
    IDictionary<string, object> Metadata { get; }

    /// <summary>
    /// The timestamp of when the message was written to the message store
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Service provider that can be used to resolve services within a handler. For a static handler function this will
    /// be the root service provider, but otherwise this will be scoped to the handler execution.
    /// </summary>
    IServiceProvider Services { get; }
}
