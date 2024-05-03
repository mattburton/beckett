using System.Transactions;
using Beckett.Events;
using Beckett.Events.Scheduling;

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

public class EventStore(IEventStorage eventStorage, IScheduledEventStorage scheduledEventStorage) : IEventStore
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    )
    {
        //TODO - populate from activity source
        var metadata = new Dictionary<string, object>();
        var eventsToAppend = new List<EventEnvelope>();
        var eventsToSchedule = new List<ScheduledEventEnvelope>();

        foreach (var @event in events)
        {
            var eventMetadata = new Dictionary<string, object>(metadata);

            if (@event is MetadataEventWrapper eventWithMetadata)
            {
                foreach (var item in eventWithMetadata.Metadata)
                {
                    eventMetadata.Add(item.Key, item.Value);
                }

                if (@event is not ScheduledEventWrapper)
                {
                    eventsToAppend.Add(new EventEnvelope(eventWithMetadata.Event, eventMetadata));

                    continue;
                }
            }

            if (@event is ScheduledEventWrapper scheduledEvent)
            {
                eventsToSchedule.Add(new ScheduledEventEnvelope(scheduledEvent.Event, eventMetadata, scheduledEvent.DeliverAt));

                continue;
            }

            eventsToAppend.Add(new EventEnvelope(@event, eventMetadata));
        }

        if (eventsToSchedule.Count == 0)
        {
            return await eventStorage.AppendToStream(streamName, expectedVersion, eventsToAppend, cancellationToken);
        }

        if (eventsToAppend.Count == 0)
        {
            await scheduledEventStorage.ScheduleEvents(streamName, eventsToSchedule, cancellationToken);

            return new AppendResult(-1);
        }

        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            await scheduledEventStorage.ScheduleEvents(streamName, eventsToSchedule, cancellationToken);

            return await eventStorage.AppendToStream(
                streamName,
                expectedVersion,
                eventsToAppend,
                cancellationToken
            );
        }
        finally
        {
            transactionScope.Complete();
        }
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken)
    {
        return eventStorage.ReadStream(streamName, options, cancellationToken);
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
