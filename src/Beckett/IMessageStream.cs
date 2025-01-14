namespace Beckett;

public interface IMessageStream
{
    /// <summary>
    /// The name of the current stream
    /// </summary>
    string StreamName { get; }

    /// <summary>
    /// The stream version (max stream position) of the current stream
    /// </summary>
    long StreamVersion { get; }

    /// <summary>
    /// The stream messages
    /// </summary>
    IReadOnlyList<IMessageContext> StreamMessages { get; }

    /// <summary>
    /// The stream messages deserialized as instances of their corresponding .NET types, skipping those where the type
    /// is not mapped
    /// </summary>
    IReadOnlyList<object> Messages { get; }

    /// <summary>
    /// Whether the current stream is empty / does not exist (has zero messages)
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Whether the current stream is not empty / exists (has one or more messages)
    /// </summary>
    bool IsNotEmpty { get; }

    /// <summary>
    /// Append messages to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    Task<IAppendResult> Append(
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Merge two streams together producing a new stream that is ordered by the global position of each message. Note
    /// that the stream version of the merged stream will be -1 since it is no longer relevant at that point in time.
    /// </summary>
    /// <param name="messageStream">The message stream to merge with the current stream</param>
    /// <returns>The merged message stream</returns>
    IMessageStream Merge(IMessageStream messageStream);

    /// <summary>
    /// Project the messages of the current stream to a representation of their state using a class that implements
    /// <see cref="IApply"/>.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <returns></returns>
    TState ProjectTo<TState>() where TState : class, IApply, new();

    /// <summary>
    /// Throw a <see cref="StreamDoesNotExistException"/> if the stream does not exist.
    /// </summary>
    /// <exception cref="StreamDoesNotExistException"></exception>
    void ThrowIfNotFound();
}

public static class MessageStreamExtensions
{
    /// <summary>
    /// Append a message to the stream using the expected version at the time it was originally read.
    /// </summary>
    /// <param name="messageStream">The message stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The append result</returns>
    public static Task<IAppendResult> Append(
        this IMessageStream messageStream,
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
    public static Task<IAppendResult> Append(
        this IMessageStream messageStream,
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
    public static Task<IAppendResult> Append(
        this IMessageStream messageStream,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => messageStream.Append(messages.Select(x => new Message(x)), cancellationToken);
}

public class MessageStream(
    string streamName,
    long streamVersion,
    IReadOnlyList<IMessageContext> streamMessages,
    AppendToStreamDelegate appendToStream
) : IMessageStream
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    public IReadOnlyList<IMessageContext> StreamMessages { get; } = streamMessages.ToList();

    public IReadOnlyList<object> Messages =>
        StreamMessages.Where(x => x.Message != null).Select(x => x.Message!).ToList();

    public bool IsEmpty => StreamMessages.Count == 0;
    public bool IsNotEmpty => StreamMessages.Count > 0;

    public Task<IAppendResult> Append(
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    ) => appendToStream(StreamName, ExpectedVersion.For(StreamVersion), messages, cancellationToken);

    public IMessageStream Merge(IMessageStream messageStream)
    {
        var streamMessages =
            StreamMessages.Concat(messageStream.StreamMessages).OrderBy(x => x.GlobalPosition).ToList();

        return new MessageStream(StreamName, -1, streamMessages, appendToStream);
    }

    public TState ProjectTo<TState>() where TState : class, IApply, new() => StreamMessages.ProjectTo<TState>();

    public void ThrowIfNotFound()
    {
        if (IsEmpty)
        {
            throw new StreamDoesNotExistException($"Stream {StreamName} does not exist.");
        }
    }
}

public delegate Task<IAppendResult> AppendToStreamDelegate(
    string streamName,
    ExpectedVersion expectedVersion,
    IEnumerable<Message> messages,
    CancellationToken cancellationToken
);
