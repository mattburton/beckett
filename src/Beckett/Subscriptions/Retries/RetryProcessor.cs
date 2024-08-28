using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Retries;

public class RetryProcessor(
    ISubscriptionRegistry subscriptionRegistry,
    BeckettOptions options,
    ICheckpointProcessor checkpointProcessor,
    IPostgresDatabase database,
    IMessageStore messageStore,
    ILogger<RetryProcessor> logger
) : IRetryProcessor
{
    public Task ManualRetry(RetryState retry, CancellationToken cancellationToken) =>
        Retry(retry, true, cancellationToken);


    public Task Retry(RetryState retry, CancellationToken cancellationToken) => Retry(retry, false, cancellationToken);

    private async Task Retry(
        RetryState retry,
        bool manualRetry,
        CancellationToken cancellationToken
    )
    {
        var subscription = subscriptionRegistry.GetSubscription(retry.Name);

        if (subscription == null)
        {
            return;
        }

        var attemptNumber = retry.Attempts + 1;

        var retryStreamName = RetryStreamName.For(retry.Id);

        try
        {
            await checkpointProcessor.Process(
                subscription,
                retry.StreamName,
                retry.StreamPosition,
                1,
                true,
                cancellationToken
            );

            logger.LogTrace(
                "Retry succeeded for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount}",
                options.Subscriptions.GroupName,
                subscription.Name,
                retry.StreamName,
                retry.StreamPosition,
                attemptNumber,
                retry.MaxRetryCount
            );

            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await database.Execute(
                new UpdateCheckpointStatus(
                    options.Subscriptions.GroupName,
                    subscription.Name,
                    retry.StreamName,
                    retry.StreamPosition,
                    CheckpointStatus.Active
                ),
                connection,
                transaction,
                cancellationToken
            );

            await messageStore.AppendToStream(
                retryStreamName,
                ExpectedVersion.Any,
                new RetrySucceeded(retry.Id, attemptNumber, DateTimeOffset.UtcNow),
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            var error = ExceptionData.From(e);

            var retryAt = attemptNumber.GetNextDelayWithExponentialBackoff(
                options.Subscriptions.Retries.InitialDelay,
                options.Subscriptions.Retries.MaxDelay
            );

            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            switch (manualRetry)
            {
                case true when attemptNumber < retry.MaxRetryCount || retry.Failed:
                    await messageStore.AppendToStream(
                        retryStreamName,
                        ExpectedVersion.Any,
                        new ManualRetryFailed(retry.Id, attemptNumber, error, DateTimeOffset.UtcNow),
                        cancellationToken
                    );
                    break;
                case false when attemptNumber < retry.MaxRetryCount:
                    await messageStore.AppendToStream(
                        retryStreamName,
                        ExpectedVersion.Any,
                        new RetryAttemptFailed(retry.Id, attemptNumber, error, retryAt, DateTimeOffset.UtcNow),
                        cancellationToken
                    );
                    break;
            }

            if (attemptNumber >= retry.MaxRetryCount && !retry.Failed)
            {
                logger.LogTrace(
                    "Retry failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - max retries exceeded, setting to Failed",
                    options.Subscriptions.GroupName,
                    subscription.Name,
                    retry.StreamName,
                    retry.StreamPosition,
                    attemptNumber,
                    retry.MaxRetryCount
                );

                await database.Execute(
                    new UpdateCheckpointStatus(
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        retry.StreamName,
                        retry.StreamPosition,
                        CheckpointStatus.Failed
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                await messageStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.Any,
                    new RetryFailed(retry.Id, attemptNumber, error, DateTimeOffset.UtcNow),
                    cancellationToken
                );
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
