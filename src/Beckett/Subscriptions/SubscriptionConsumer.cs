using Beckett.Database;
using Beckett.Database.Queries;

namespace Beckett.Subscriptions;

public class SubscriptionConsumer(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    SubscriptionOptions options,
    CancellationToken stoppingToken
) : ISubscriptionConsumer
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

            await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

            var checkpoint = await database.Execute(
                new LockNextAvailableCheckpoint(),
                connection,
                transaction,
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
                transaction,
                subscription,
                checkpoint.StreamName,
                checkpoint.StreamPosition + 1,
                options.BatchSize,
                true,
                stoppingToken
            );

            await transaction.CommitAsync(stoppingToken);
        }
    }
}
