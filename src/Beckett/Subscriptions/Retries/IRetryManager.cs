namespace Beckett.Subscriptions.Retries;

public interface IRetryManager
{
    Task Retry(
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    );
}
