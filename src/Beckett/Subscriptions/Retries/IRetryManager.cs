namespace Beckett.Subscriptions.Retries;

public interface IRetryManager
{
    Task Retry(
        string subscriptionName,
        string topic,
        string streamId,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    );
}
