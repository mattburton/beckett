using Beckett.MessageStorage;

namespace Beckett;

public readonly struct MessageStream(
    string streamName,
    long streamVersion,
    IReadOnlyList<MessageResult> streamMessages,
    AppendToStreamDelegate appendToStream)
{
    /// <summary>
    /// The name of the current stream
    /// </summary>
    public string StreamName { get; } = streamName;

    /// <summary>
    /// The stream version (max stream position) of the current stream
    /// </summary>
    public long StreamVersion { get; } = streamVersion;

    public IReadOnlyList<MessageResult> RawMessages { get; } = streamMessages.ToList();

    /// <summary>
    /// The stream messages
    /// </summary>
    public IReadOnlyList<object> Messages => RawMessages.Select(x => x.Message).ToList();

    /// <summary>
    /// Whether the current stream is empty / does not exist (has zero messages)
    /// </summary>
    public bool IsEmpty => Messages.Count == 0;

    /// <summary>
    /// Whether the current stream is not empty / exists (has one or more messages)
    /// </summary>
    public bool IsNotEmpty => Messages.Count > 0;

    /// <summary>
    /// Append a message to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public Task<AppendResult> Append(
        object message,
        CancellationToken cancellationToken
    ) => Append([message], cancellationToken);

    /// <summary>
    /// Append messages to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public Task<AppendResult> Append(
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => appendToStream(StreamName, ExpectedVersion.For(StreamVersion), messages, cancellationToken);

    /// <summary>
    /// Merge two streams together producing a new stream that is ordered by the global position of each message. Note
    /// that the stream version of the merged stream will be -1 since it is no longer relevant at that point in time.
    /// </summary>
    /// <param name="messageStream">The message stream to merge with the current stream</param>
    /// <returns>The merged message stream</returns>
    public MessageStream Merge(MessageStream messageStream)
    {
        var streamMessages =
            RawMessages.Concat(messageStream.RawMessages).OrderBy(x => x.GlobalPosition).ToList();

        return new MessageStream(StreamName, -1, streamMessages, appendToStream);
    }

    /// <summary>
    /// Project the messages of the current stream to a representation of their state using a class that implements
    /// <see cref="IApply"/>.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <returns></returns>
    public TState ProjectTo<TState>() where TState : IApply, new() => Messages.ProjectTo<TState>();

    /// <summary>
    /// Throw a <see cref="StreamDoesNotExistException"/> if the stream does not exist.
    /// </summary>
    /// <exception cref="StreamDoesNotExistException"></exception>
    public void ThrowIfNotFound()
    {
        if (IsEmpty)
        {
            throw new StreamDoesNotExistException($"Stream {StreamName} does not exist.");
        }
    }
}

public delegate Task<AppendResult> AppendToStreamDelegate(
    string streamName,
    ExpectedVersion expectedVersion,
    IEnumerable<object> messages,
    CancellationToken cancellationToken
);
