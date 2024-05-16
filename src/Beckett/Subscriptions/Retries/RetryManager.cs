using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Models;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries;

public class RetryManager(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    SubscriptionOptions options,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    IMessageStore messageStore
) : IRetryManager
{
    public async Task Retry(
        string subscriptionName,
        string topic,
        string streamId,
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

        var retryStreamId = RetryStreamId.For(subscriptionName, topic, streamId, streamPosition);
        var retryAt = attempts.GetNextDelayWithExponentialBackoff();

        attempts++;

        var reachedMaxAttempts = attempts >= subscription.MaxRetryCount;

        try
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(options.ApplicationName, subscription.Name, topic, streamId),
                connection,
                transaction,
                cancellationToken
            );

            if (checkpoint == null || checkpoint.Status is CheckpointStatus.Active)
            {
                return;
            }

            await subscriptionStreamProcessor.Process(
                connection,
                transaction,
                subscription,
                topic,
                streamId,
                checkpoint.StreamPosition,
                1,
                reachedMaxAttempts,
                true,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            await messageStore.AppendToStream(
                RetryConstants.Topic,
                retryStreamId,
                ExpectedVersion.StreamExists,
                new SubscriptionRetrySucceeded(
                    subscriptionName,
                    topic,
                    streamId,
                    streamPosition,
                    attempts + 1,
                    DateTimeOffset.UtcNow
                ),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            if (reachedMaxAttempts)
            {
                await messageStore.AppendToStream(
                    RetryConstants.Topic,
                    retryStreamId,
                    ExpectedVersion.StreamExists,
                    new SubscriptionRetryFailed(
                        subscriptionName,
                        topic,
                        streamId,
                        streamPosition,
                        attempts,
                        ExceptionData.From(ex),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }
            else
            {
                await messageStore.AppendToStream(
                    RetryConstants.Topic,
                    retryStreamId,
                    ExpectedVersion.StreamExists,
                    new SubscriptionRetryError(
                        subscriptionName,
                        topic,
                        streamId,
                        streamPosition,
                        attempts,
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
