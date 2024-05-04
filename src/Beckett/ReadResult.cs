namespace Beckett;

public readonly struct ReadResult(IReadOnlyList<object> events, long streamVersion)
{
    public IReadOnlyList<object> Events { get; } = events;
    public long StreamVersion { get; } = streamVersion;

    public bool IsEmpty => Events.Count == 0;

    public TState ProjectTo<TState>() where TState : IState, new() => Events.ProjectTo<TState>();
}
