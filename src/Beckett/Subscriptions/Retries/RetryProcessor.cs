using Beckett.Database;
using Beckett.Subscriptions.Retries.Queries;

namespace Beckett.Subscriptions.Retries;

public class RetryProcessor(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    BeckettOptions options,
    ISubscriptionStreamProcessor subscriptionStreamProcessor
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
        }
        catch (Exception e)
        {
            var error = ExceptionData.From(e).ToJson();

            if (manualRetry)
            {
                await database.Execute(
                    new RecordRetryEvent(id, RetryStatus.ManualRetryFailed, attemptNumber, error: error),
                    cancellationToken
                );

                return;
            }

            if (attemptNumber >= maxRetryCount)
            {
                await database.Execute(
                    new RecordRetryEvent(id, RetryStatus.Failed, attemptNumber, error: error),
                    cancellationToken
                );
            }
            else
            {
                var retryAt = attemptNumber.GetNextDelayWithExponentialBackoff(
                    options.Subscriptions.Retries.InitialDelay,
                    options.Subscriptions.Retries.MaxDelay
                );

                await database.Execute(
                    new RecordRetryEvent(id, RetryStatus.Scheduled, attemptNumber, retryAt, error),
                    cancellationToken
                );
            }
        }
    }
}
