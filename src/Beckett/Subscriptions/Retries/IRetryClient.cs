namespace Beckett.Subscriptions.Retries;

public interface IRetryClient
{
    Task BulkDelete(Guid[] retryIds, CancellationToken cancellationToken);

    Task BulkRetry(Guid[] retryIds, CancellationToken cancellationToken);

    Task ManualRetry(Guid id, CancellationToken cancellationToken);

    Task DeleteRetry(Guid id, CancellationToken cancellationToken);
}
