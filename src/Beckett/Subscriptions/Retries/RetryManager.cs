using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries;

public class RetryManager(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    IMessageStore messageStore
) : IRetryManager
{
    public async Task Retry(
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    )
    {
        var subscription = subscriptionRegistry.GetSubscription(subscriptionName);

        if (subscription == null)
        {
            return;
        }

        var retryStreamName = RetryStreamName.For(subscriptionName, streamName);
        var retryAt = attempts.GetNextDelayWithExponentialBackoff();

        try
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(subscription.Name, streamName),
                connection,
                transaction,
                cancellationToken
            );

            if (checkpoint is not { Blocked: true })
            {
                return;
            }

            await subscriptionStreamProcessor.Process(
                connection,
                transaction,
                subscription,
                streamName,
                checkpoint.StreamPosition,
                1,
                false,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            await messageStore.AppendToStream(
                retryStreamName,
                ExpectedVersion.StreamExists,
                new SubscriptionRetrySucceeded(
                    subscriptionName,
                    streamName,
                    streamPosition,
                    attempts + 1,
                    DateTimeOffset.UtcNow
                ),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            if (attempts >= subscription.MaxRetryCount)
            {
                await messageStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new SubscriptionRetryFailed(
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attempts + 1,
                        ExceptionData.From(ex),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }
            else
            {
                await messageStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new SubscriptionRetryError(
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attempts + 1,
                        ExceptionData.From(ex),
                        retryAt,
                        DateTimeOffset.UtcNow
                    ).ScheduleAt(retryAt),
                    cancellationToken
                );
            }
        }
    }
}
