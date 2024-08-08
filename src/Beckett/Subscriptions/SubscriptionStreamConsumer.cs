using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class SubscriptionStreamConsumer(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    BeckettOptions options,
    ILogger<SubscriptionStreamConsumer> logger,
    CancellationToken stoppingToken
) : ISubscriptionStreamConsumer
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
                        options.Subscriptions.ReservationTimeout
                    ),
                    stoppingToken
                );

                if (checkpoint == null)
                {
                    break;
                }

                var subscription = subscriptionRegistry.GetSubscription(checkpoint.Name);

                if (subscription == null)
                {
                    continue;
                }

                await subscriptionStreamProcessor.Process(
                    subscription,
                    checkpoint.StreamName,
                    checkpoint.StreamPosition + 1,
                    options.Subscriptions.SubscriptionStreamBatchSize,
                    false,
                    stoppingToken
                );
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing subscription stream");

                throw;
            }
        }
    }
}
