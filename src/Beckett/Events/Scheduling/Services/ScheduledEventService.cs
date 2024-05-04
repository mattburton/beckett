using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Events.Scheduling.Services;

public class ScheduledEventService(
    BeckettOptions options,
    IScheduledEventStorage storage,
    IEventStore eventStore,
    ILogger<ScheduledEventService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await storage.DeliverScheduledEvents(
                    options.Events.ScheduledEventBatchSize,
                    DeliverScheduledEvents,
                    stoppingToken
                );
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

    private async Task DeliverScheduledEvents(
        IReadOnlyList<IScheduledEventContext> scheduledEvents,
        CancellationToken cancellationToken
    )
    {
        foreach (var scheduledEventsForStream in scheduledEvents.GroupBy(x => x.StreamName))
        {
            var events = scheduledEventsForStream.Select(x => x.Data.WithMetadata(x.Metadata));

            await eventStore.AppendToStream(
                scheduledEventsForStream.Key,
                ExpectedVersion.Any,
                events,
                cancellationToken
            );
        }
    }
}
