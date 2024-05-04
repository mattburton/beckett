using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Events;

namespace Beckett.ScheduledEvents;

public class EventScheduler(
    IPostgresDatabase database,
    IEventSerializer eventSerializer
) : IEventScheduler
{
    public async Task ScheduleEvents(
        string streamName,
        IEnumerable<ScheduledEventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        var scheduledEvents = events.Select(x => ScheduledEventType.From(
            x.Event,
            x.Metadata,
            x.DeliverAt,
            eventSerializer
        )).ToArray();

        await database.Execute(new ScheduleEvents(streamName, scheduledEvents), cancellationToken);
    }
}
