namespace Beckett.Subscriptions.Retries;

public interface IRetryClient
{
    Task BulkRetry(Guid[] retryIds, CancellationToken cancellationToken);

    Task ManualRetry(Guid id, CancellationToken cancellationToken);
}
