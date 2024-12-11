namespace Beckett.Subscriptions.Retries;

public interface IRetryClient
{
    Task BulkRetry(long[] ids, CancellationToken cancellationToken);

    Task BulkSkip(long[] ids, CancellationToken cancellationToken);

    Task Retry(long id, CancellationToken cancellationToken);

    Task Skip(long id, CancellationToken cancellationToken);
}
