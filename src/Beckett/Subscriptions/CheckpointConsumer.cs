using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumer(
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    BeckettOptions options,
    ILogger<CheckpointConsumer> logger,
    CancellationToken stoppingToken
) : ICheckpointConsumer
{
    private Task _task = null!;

    public void StartPolling()
    {
        if (_task is { IsCompleted: false })
        {
            return;
        }

        _task = Poll();
    }

    private async Task Poll()
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var checkpoint = await database.Execute(
                    new ReserveNextAvailableCheckpoint(
                        options.Subscriptions.GroupName,
                        options.Subscriptions.ReservationTimeout,
                        options.Postgres
                    ),
                    stoppingToken
                );

                if (checkpoint == null)
                {
                    break;
                }

                var subscription = SubscriptionRegistry.GetSubscription(checkpoint.Name);

                if (subscription == null)
                {
                    logger.LogTrace(
                        "Subscription {Name} not registered for group {GroupName} - skipping [Checkpoint: {CheckpoinId}]",
                        checkpoint.Name,
                        options.Subscriptions.GroupName,
                        checkpoint.Id
                    );

                    continue;
                }

                logger.LogTrace(
                    "Processing checkpoint {CheckpointId} at position {StreamPosition} and version {StreamVersion}",
                    checkpoint.Id,
                    checkpoint.StreamPosition,
                    checkpoint.StreamVersion
                );

                await checkpointProcessor.Process(
                    checkpoint,
                    subscription,
                    stoppingToken
                );
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing checkpoint");

                throw;
            }
        }
    }
}
