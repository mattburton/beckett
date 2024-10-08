namespace Beckett.Subscriptions.Retries;

public interface IRetryClient
{
    Task BulkRetry(long[] ids, CancellationToken cancellationToken);

    Task Retry(long id, CancellationToken cancellationToken);
}
