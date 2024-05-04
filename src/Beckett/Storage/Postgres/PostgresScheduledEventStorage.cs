using Beckett.Events;
using Beckett.Events.Scheduling;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;

namespace Beckett.Storage.Postgres;

public class PostgresScheduledEventStorage(
    BeckettOptions options,
    IPostgresDatabase database,
    EventSerializer eventSerializer
) : IScheduledEventStorage
{
    public async Task ScheduleEvents(
        string streamName,
        IEnumerable<ScheduledEventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var newStreamEvents = events.Select(x => NewScheduledEvent.From(
            x.Event,
            x.Metadata,
            x.DeliverAt,
            eventSerializer
        )).ToArray();

        await ScheduleEventsQuery.Execute(
            connection,
            options.Postgres.Schema,
            streamName,
            newStreamEvents,
            cancellationToken
        );
    }

    public async Task DeliverScheduledEvents(
        int batchSize,
        DeliverScheduledEventsCallback callback,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var results = await GetScheduledEventsToDeliverQuery.Execute(
            connection,
            transaction,
            options.Postgres.Schema,
            options.Events.ScheduledEventBatchSize,
            cancellationToken
        );

        var scheduledEvents = new List<IScheduledEventContext>();

        foreach (var streamGroup in results.GroupBy(x => x.StreamName))
        {
            foreach (var scheduledEvent in streamGroup)
            {
                var (_, data, metadata) = PostgresEventDeserializer.DeserializeAll(scheduledEvent, options);

                scheduledEvents.Add(new ScheduledEventContext(scheduledEvent.StreamName, data, metadata));
            }
        }

        await callback(scheduledEvents, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
