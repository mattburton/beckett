namespace Beckett.Subscriptions.Retries;

public interface IRetryManager
{
    Task StartRetry(
        Guid id,
        string applicationName,
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    );

    Task RecordFailure(
        Guid id,
        string applicationName,
        string subscriptionName,
        string streamName,
        long streamPosition,
        string lastError,
        CancellationToken cancellationToken
    );

    Task Retry(
        Guid id,
        string applicationName,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    );


    Task ManualRetry(Guid id, CancellationToken cancellationToken);

    Task DeleteRetry(Guid id, CancellationToken cancellationToken);
}
