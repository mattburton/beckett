using Beckett.Messages;

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

public readonly struct SessionReadResult(
    string streamName,
    long streamVersion,
    IReadOnlyList<object> messages,
    SessionAppendToStreamDelegate appendToStream)
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    public IReadOnlyList<object> Messages { get; } = messages;

    public bool IsEmpty => Messages.Count == 0;

    public void Append(
        object message
    ) => Append([message]);

    public void Append(
        IEnumerable<object> messages
    ) => appendToStream(StreamName, ExpectedVersion.For(StreamVersion), messages);

    public TState ProjectTo<TState>() where TState : IApply, new() => Messages.ProjectTo<TState>();
}
