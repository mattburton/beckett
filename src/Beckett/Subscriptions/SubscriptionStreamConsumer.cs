using Beckett.Database;
using Beckett.Database.Queries;

namespace Beckett.Subscriptions;

public class SubscriptionStreamConsumer(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    BeckettOptions options,
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
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(stoppingToken);

            var checkpoint = await database.Execute(
                new ReserveNextAvailableCheckpoint(
                    options.Subscriptions.GroupName,
                    options.Subscriptions.CheckpointReservationTimeout
                ),
                connection,
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
                connection,
                subscription,
                checkpoint.Id,
                checkpoint.StreamName,
                checkpoint.StreamPosition + 1,
                options.Subscriptions.SubscriptionStreamBatchSize,
                false,
                stoppingToken
            );
        }
    }
}
