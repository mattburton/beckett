using Beckett.MessageStorage;

namespace Beckett;

public readonly struct MessageStream(
    string streamName,
    long streamVersion,
    IReadOnlyList<MessageResult> streamMessages,
    AppendToStreamDelegate appendToStream)
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    internal IReadOnlyList<MessageResult> RawMessages { get; } = streamMessages.ToList();
    public IReadOnlyList<object> Messages => RawMessages.Select(x => x.Message).ToList();

    public bool IsEmpty => Messages.Count == 0;

    public bool IsNotEmpty => Messages.Count > 0;

    public Task<AppendResult> Append(
        object message,
        CancellationToken cancellationToken
    ) => Append([message], cancellationToken);

    public Task<AppendResult> Append(
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => appendToStream(StreamName, ExpectedVersion.For(StreamVersion), messages, cancellationToken);

    public MessageStream Merge(MessageStream messageStream)
    {
        var streamMessages =
            RawMessages.Concat(messageStream.RawMessages).OrderBy(x => x.GlobalPosition).ToList();

        return new MessageStream(StreamName, StreamVersion, streamMessages, appendToStream);
    }

    public TState ProjectTo<TState>() where TState : IApply, new() => Messages.ProjectTo<TState>();

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
