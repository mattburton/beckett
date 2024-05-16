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
    private bool _pendingRequest;

    public void StartPolling()
    {
        if (_task is { IsCompleted: false })
        {
            _pendingRequest = true;

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
                new LockNextAvailableCheckpoint(options.ApplicationName),
                connection,
                transaction,
                stoppingToken
            );

            if (checkpoint == null)
            {
                if (!_pendingRequest)
                {
                    break;
                }

                _pendingRequest = false;

                continue;
            }

            var subscription = subscriptionRegistry.GetSubscription(checkpoint.Name);

            if (subscription == null)
            {
                continue;
            }

            var movedToFailedOnError = subscription.MaxRetryCount == 0;

            await subscriptionStreamProcessor.Process(
                connection,
                transaction,
                subscription,
                checkpoint.StreamName,
                checkpoint.StreamPosition + 1,
                options.BatchSize,
                movedToFailedOnError,
                false,
                stoppingToken
            );

            await transaction.CommitAsync(stoppingToken);
        }
    }
}
