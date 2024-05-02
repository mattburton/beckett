using Beckett.Events;

namespace Beckett;

public interface IEventStore
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}

public class EventStore(IEventStorage storage) : IEventStore
{
    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    )
    {
        return storage.AppendToStream(streamName, expectedVersion, events, cancellationToken);
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken)
    {
        return storage.ReadStream(streamName, options, cancellationToken);
    }
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

    public long? StartingStreamPosition { get; set; }
    public long? EndingGlobalPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }
}

public readonly struct AppendResult(long streamVersion)
{
    public long StreamVersion { get; } = streamVersion;
}

public readonly struct ReadResult(IReadOnlyList<object> events, long streamVersion)
{
    public IReadOnlyList<object> Events { get; } = events;
    public long StreamVersion { get; } = streamVersion;

    public bool IsEmpty => Events.Count == 0;

    public TState ProjectTo<TState>() where TState : IState, new() => Events.ProjectTo<TState>();
}
