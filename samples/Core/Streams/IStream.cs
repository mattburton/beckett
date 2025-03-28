using Beckett;

namespace Core.Streams;

public interface IStream
{
    /// <summary>
    /// The name of the current stream
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The stream version (max stream position) of the current stream
    /// </summary>
    long Version { get; }

    /// <summary>
    /// The stream messages
    /// </summary>
    IReadOnlyList<IMessageContext> Messages { get; }

    /// <summary>
    /// Whether the current stream is empty / does not exist (has zero messages)
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Whether the current stream is not empty / exists (has one or more messages)
    /// </summary>
    bool IsNotEmpty { get; }

    /// <summary>
    /// Merge two streams together producing a new stream that is ordered by the global position of each message. Note
    /// that the stream version of the merged stream will be -1 since it is no longer relevant at that point in time.
    /// </summary>
    /// <param name="stream">The message stream to merge with the current stream</param>
    /// <returns>The merged message stream</returns>
    IStream Merge(IStream stream);

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

public class Stream(IMessageStream source) : IStream
{
    public string Name => source.StreamName;
    public long Version => source.StreamVersion;
    public IReadOnlyList<IMessageContext> Messages => source.StreamMessages;
    public bool IsEmpty => source.IsEmpty;
    public bool IsNotEmpty => source.IsNotEmpty;

    public IStream Merge(IStream stream)
    {
        var messages = Messages.Concat(stream.Messages).OrderBy(x => x.GlobalPosition).ToList();

        return new Stream(new MessageStream(Name, -1, messages, NoOpAppendToStream));
    }

    public TState ProjectTo<TState>() where TState : class, IApply, new() => source.ProjectTo<TState>();

    public void ThrowIfNotFound() => source.ThrowIfNotFound();

    private static Task<IAppendResult> NoOpAppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<IAppendResult>(new AppendResult(-1));
    }
}
