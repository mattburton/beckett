using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.ScheduledEvents.Services;

public class ScheduledEventService(
    IPostgresDatabase database,
    ScheduledEventOptions options,
    IPostgresEventDeserializer eventDeserializer,
    IEventStore eventStore,
    ILogger<ScheduledEventService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var results = await database.Execute(
                    new GetScheduledEventsToDeliver(options.BatchSize),
                    connection,
                    transaction,
                    stoppingToken
                );

                var scheduledEvents = new List<IScheduledEventContext>();

                foreach (var streamGroup in results.GroupBy(x => x.StreamName))
                {
                    foreach (var scheduledEvent in streamGroup)
                    {
                        var (data, metadata) = eventDeserializer.DeserializeAll(scheduledEvent);

                        scheduledEvents.Add(new ScheduledEventContext(scheduledEvent.StreamName, data, metadata));
                    }
                }

                foreach (var scheduledEventsForStream in scheduledEvents.GroupBy(x => x.StreamName))
                {
                    var events = scheduledEventsForStream.Select(x => x.Data.WithMetadata(x.Metadata));

                    await eventStore.AppendToStream(
                        scheduledEventsForStream.Key,
                        ExpectedVersion.Any,
                        events,
                        stoppingToken
                    );
                }

                await transaction.CommitAsync(stoppingToken);

                await Task.Delay(options.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled error delivering scheduled events - will try again in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
