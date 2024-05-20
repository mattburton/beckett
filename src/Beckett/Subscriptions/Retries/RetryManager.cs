using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Models;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries;

public class RetryManager(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    BeckettOptions options,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    IMessageStore messageStore,
    IMessageScheduler messageScheduler
) : IRetryManager
{
    public async Task CreateRetry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    )
    {
        await messageScheduler.ScheduleMessage(
            RetryStreamName.For(id),
            new RetryStarted(
                id,
                options.ApplicationName,
                subscriptionName,
                streamName,
                streamPosition,
                DateTimeOffset.UtcNow
            ),
            TimeSpan.FromSeconds(10),
            cancellationToken
        );
    }

    public async Task Retry(
        Guid id,
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

        var retryStreamName = RetryStreamName.For(id);

        attempts += 1;

        var reachedMaxAttempts = attempts >= subscription.MaxRetryCount;

        try
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(options.ApplicationName, subscription.Name, streamName),
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
                streamName,
                checkpoint.StreamPosition,
                1,
                reachedMaxAttempts,
                true,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            await messageStore.AppendToStream(
                retryStreamName,
                ExpectedVersion.StreamExists,
                new RetrySucceeded(
                    id,
                    options.ApplicationName,
                    subscriptionName,
                    streamName,
                    streamPosition,
                    attempts,
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
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new RetryFailed(
                        id,
                        options.ApplicationName,
                        subscriptionName,
                        streamName,
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
                var delay = attempts.GetNextDelayWithExponentialBackoff();

                await messageScheduler.ScheduleMessage(
                    retryStreamName,
                    new RetryError(
                        id,
                        options.ApplicationName,
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attempts,
                        ExceptionData.From(ex),
                        DateTimeOffset.UtcNow
                    ),
                    delay,
                    cancellationToken
                );
            }
        }
    }
}
