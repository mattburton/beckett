namespace Beckett.ScheduledEvents;

public interface IEventScheduler
{
    Task ScheduleEvents(
        string streamName,
        IEnumerable<ScheduledEventEnvelope> events,
        CancellationToken cancellationToken
    );
}
