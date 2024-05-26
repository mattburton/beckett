namespace Beckett.Subscriptions.Retries;

public interface IRetryManager
{
    Task CreateRetry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    );

    Task Retry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    );
}
