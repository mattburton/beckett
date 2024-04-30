namespace Beckett;

public interface IEventStore
{
    Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    );

    Task<IReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}

public readonly record struct ExpectedVersion(long Value)
{
    public static readonly ExpectedVersion StreamDoesNotExist = new(0);
    public static readonly ExpectedVersion StreamExists = new(-1);
    public static readonly ExpectedVersion Any = new(-2);
}

public class ReadOptions
{
    public static readonly ReadOptions Default = new();

    //TODO - add query support for this
    public long? ExpectedStreamVersion { get; set; }
    public long? StartingStreamPosition { get; set; }
    public long? EndingGlobalPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }
}

public interface IAppendResult
{
    long StreamVersion { get; }
}

internal record AppendResult(long StreamVersion) : IAppendResult;

public interface IReadResult
{
    IReadOnlyList<object> Events { get; }
    long StreamVersion { get; }

    bool IsEmpty => Events.Count == 0;

    TState ProjectTo<TState>() where TState : IState, new() => Events.ProjectTo<TState>();
}

internal record ReadResult(IReadOnlyList<object> Events, long StreamVersion) : IReadResult;
