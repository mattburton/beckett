using Beckett.Database;
using Beckett.Subscriptions.Retries.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Retries;

public class RetryProcessor(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    BeckettOptions options,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    ILogger<RetryProcessor> logger
) : IRetryProcessor
{
    public async Task Retry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        int maxRetryCount,
        bool manualRetry,
        CancellationToken cancellationToken
    )
    {
        var subscription = subscriptionRegistry.GetSubscription(subscriptionName);

        if (subscription == null)
        {
            return;
        }

        var attemptNumber = attempts + 1;

        try
        {
            await subscriptionStreamProcessor.Process(
                subscription,
                streamName,
                streamPosition,
                1,
                true,
                cancellationToken
            );

            await database.Execute(new RecordRetryEvent(id, RetryStatus.Succeeded, attemptNumber), cancellationToken);

            logger.LogTrace(
                "Retry succeeded for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount}",
                options.Subscriptions.GroupName,
                subscription.Name,
                streamName,
                streamPosition,
                attemptNumber,
                maxRetryCount
            );
        }
        catch (Exception e)
        {
            var error = ExceptionData.From(e).ToJson();

            var retryAt = attemptNumber.GetNextDelayWithExponentialBackoff(
                options.Subscriptions.Retries.InitialDelay,
                options.Subscriptions.Retries.MaxDelay
            );

            if (manualRetry)
            {
                if (attemptNumber >= maxRetryCount)
                {
                    logger.LogTrace(
                        "Manual retry failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - max retries exceeded, setting to Failed",
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        streamName,
                        streamPosition,
                        attemptNumber,
                        maxRetryCount
                    );

                    await database.Execute(
                        new RecordRetryEvent(id, RetryStatus.Failed, attemptNumber, error: error),
                        cancellationToken
                    );
                }
                else if (attemptNumber < maxRetryCount)
                {
                    logger.LogTrace(
                        "Manual retry failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - will retry at {RetryAt}",
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        streamName,
                        streamPosition,
                        attemptNumber,
                        maxRetryCount,
                        retryAt
                    );

                    await database.Execute(
                        new RecordRetryEvent(id, RetryStatus.ManualRetryFailed, attemptNumber, retryAt, error),
                        cancellationToken
                    );
                }
                else
                {
                    logger.LogTrace(
                        "Manual retry failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount}",
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        streamName,
                        streamPosition,
                        attemptNumber,
                        maxRetryCount
                    );

                    await database.Execute(
                        new RecordRetryEvent(id, RetryStatus.ManualRetryFailed, attemptNumber, error: error),
                        cancellationToken
                    );
                }

                return;
            }

            if (attemptNumber >= maxRetryCount)
            {
                logger.LogTrace(
                    "Retry failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - max retries exceeded, setting to Failed",
                    options.Subscriptions.GroupName,
                    subscription.Name,
                    streamName,
                    streamPosition,
                    attemptNumber,
                    maxRetryCount
                );

                await database.Execute(
                    new RecordRetryEvent(id, RetryStatus.Failed, attemptNumber, error: error),
                    cancellationToken
                );
            }
            else
            {
                logger.LogTrace(
                    "Retry failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - will retry at {RetryAt}",
                    options.Subscriptions.GroupName,
                    subscription.Name,
                    streamName,
                    streamPosition,
                    attemptNumber,
                    maxRetryCount,
                    retryAt
                );

                await database.Execute(
                    new RecordRetryEvent(id, RetryStatus.Scheduled, attemptNumber, retryAt, error),
                    cancellationToken
                );
            }
        }
    }
}
