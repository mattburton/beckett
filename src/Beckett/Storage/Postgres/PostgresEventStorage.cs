using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;
using Microsoft.Extensions.Logging;

namespace Beckett.Storage.Postgres;

public class PostgresEventStorage(
    BeckettOptions beckett,
    IPostgresDatabase database,
    ILogger<PostgresEventStorage> logger
) : IEventStorage
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<EventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var newStreamEvents = events.Select(x => NewStreamEvent.From(x.Event, x.Metadata, x.DeliverAt)).ToArray();

        var streamVersion = await AppendToStreamQuery.Execute(
            connection,
            beckett.Postgres.Schema,
            streamName,
            expectedVersion.Value,
            newStreamEvents,
            beckett.Postgres.EnableNotifications,
            cancellationToken
        );

        return new AppendResult(streamVersion);
    }

    public IEnumerable<Task> ConfigureBackgroundService(CancellationToken stoppingToken)
    {
        yield return DeliverScheduledEvents(stoppingToken);
    }

    public async Task DeliverScheduledEvents(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                await connection.OpenAsync(cancellationToken);

                await DeliverScheduledEventsQuery.Execute(
                    connection,
                    beckett.Postgres.Schema,
                    beckett.Postgres.EnableNotifications,
                    cancellationToken
                );

                await Task.Delay(beckett.Events.ScheduledEventPollingInterval, cancellationToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Database error - will try in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    public async Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var streamEvents = await ReadStreamQuery.Execute(
            connection,
            beckett.Postgres.Schema,
            streamName,
            options,
            cancellationToken
        );

        //TODO update query to always return actual stream version regardless of read options supplied
        var streamVersion = streamEvents.Count == 0 ? 0 : streamEvents[^1].StreamPosition;

        var events = streamEvents.Select(PostgresEventDeserializer.Deserialize).ToList();

        return new ReadResult(events, streamVersion);
    }
}
