using System.Transactions;
using Beckett.Events;
using Beckett.ScheduledEvents;

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

public class EventStore(IEventStorage eventStorage, IEventScheduler eventScheduler) : IEventStore
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
            await eventScheduler.ScheduleEvents(streamName, eventsToSchedule, cancellationToken);

            return new AppendResult(-1);
        }

        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        await eventScheduler.ScheduleEvents(streamName, eventsToSchedule, cancellationToken);

        var result = await eventStorage.AppendToStream(
            streamName,
            expectedVersion,
            eventsToAppend,
            cancellationToken
        );

        transactionScope.Complete();

        return result;
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken)
    {
        return eventStorage.ReadStream(streamName, options, cancellationToken);
    }
}
