namespace Beckett;

public interface IMessageStore
{
    /// <summary>
    /// Append a message to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Append a message to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        Message message,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Append messages to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Append messages to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Read a message stream, returning an entire message stream in order from beginning to end
    /// </summary>
    /// <param name="streamName">The name of the stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<MessageStream> ReadStream(
        string streamName,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Read a message stream passing in options to control how the stream is read - starting / ending positions,
    /// backwards or forwards, etc...
    /// </summary>
    /// <param name="streamName">The name of the stream</param>
    /// <param name="options">The read options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<MessageStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}
