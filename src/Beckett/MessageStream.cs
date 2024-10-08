namespace Beckett;

public readonly struct MessageStream(
    string streamName,
    long streamVersion,
    IReadOnlyList<StreamMessage> streamMessages,
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

    /// <summary>
    /// The stream messages
    /// </summary>
    public IReadOnlyList<StreamMessage> StreamMessages { get; } = streamMessages.ToList();

    /// <summary>
    /// The stream messages deserialized as instances of their corresponding .NET types, skipping those where the type
    /// is not mapped
    /// </summary>
    public IReadOnlyList<object> Messages =>
        StreamMessages.Where(x => x.Message != null).Select(x => x.Message!).ToList();

    /// <summary>
    /// Whether the current stream is empty / does not exist (has zero messages)
    /// </summary>
    public bool IsEmpty => StreamMessages.Count == 0;

    /// <summary>
    /// Whether the current stream is not empty / exists (has one or more messages)
    /// </summary>
    public bool IsNotEmpty => StreamMessages.Count > 0;

    /// <summary>
    /// Append messages to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public Task<AppendResult> Append(
        IEnumerable<Message> messages,
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
            StreamMessages.Concat(messageStream.StreamMessages).OrderBy(x => x.GlobalPosition).ToList();

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
    IEnumerable<Message> messages,
    CancellationToken cancellationToken
);

public static class MessageStreamExtensions
{
    /// <summary>
    /// Append a message to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messageStream">The message stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public static Task<AppendResult> Append(
        this MessageStream messageStream,
        object message,
        CancellationToken cancellationToken
    ) => messageStream.Append([new Message(message)], cancellationToken);

    /// <summary>
    /// Append a message to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messageStream">The message stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public static Task<AppendResult> Append(
        this MessageStream messageStream,
        Message message,
        CancellationToken cancellationToken
    ) => messageStream.Append([message], cancellationToken);

    /// <summary>
    /// Append messages to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messageStream">The message stream</param>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public static Task<AppendResult> Append(
        this MessageStream messageStream,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => messageStream.Append(messages.Select(x => new Message(x)), cancellationToken);
}
