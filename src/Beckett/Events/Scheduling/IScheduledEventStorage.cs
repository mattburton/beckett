namespace Beckett.Events.Scheduling;

public interface IScheduledEventStorage
{
    Task ScheduleEvents(
        string streamName,
        IEnumerable<ScheduledEventEnvelope> events,
        CancellationToken cancellationToken
    );

    Task DeliverScheduledEvents(int batchSize, DeliverScheduledEventsCallback callback, CancellationToken cancellationToken);
}

public delegate Task DeliverScheduledEventsCallback(
    IReadOnlyList<IScheduledEventContext> scheduledEvents,
    CancellationToken cancellationToken
);
