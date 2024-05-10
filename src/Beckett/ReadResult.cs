namespace Beckett;

public readonly struct ReadResult(IReadOnlyList<object> messages, long streamVersion)
{
    public IReadOnlyList<object> Messages { get; } = messages;
    public long StreamVersion { get; } = streamVersion;

    public bool IsEmpty => Messages.Count == 0;

    public TState ProjectTo<TState>() where TState : IState, new() => Messages.ProjectTo<TState>();
}
