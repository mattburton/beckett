namespace Beckett;

public readonly struct ReadResult(
    string streamName,
    long streamVersion,
    IReadOnlyList<object> messages,
    AppendToStreamDelegate appendToStream)
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    public IReadOnlyList<object> Messages { get; } = messages;

    public bool IsEmpty => Messages.Count == 0;

    public Task<AppendResult> Append(
        object message,
        CancellationToken cancellationToken
    ) => Append([message], cancellationToken);

    public Task<AppendResult> Append(
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => appendToStream(StreamName, ExpectedVersion.For(StreamVersion), messages, cancellationToken);

    public TState ProjectTo<TState>() where TState : IApply, new() => Messages.ProjectTo<TState>();
}

public delegate Task<AppendResult> AppendToStreamDelegate(
    string streamName,
    ExpectedVersion expectedVersion,
    IEnumerable<object> messages,
    CancellationToken cancellationToken
);
