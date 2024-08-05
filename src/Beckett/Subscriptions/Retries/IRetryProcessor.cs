namespace Beckett.Subscriptions.Retries;

public interface IRetryProcessor
{
    Task Retry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        int maxRetryCount,
        bool manualRetry,
        CancellationToken cancellationToken
    );
}
