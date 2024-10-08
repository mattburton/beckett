namespace Beckett.Subscriptions.Retries;

public class RetryClient : IRetryClient
{
    public Task BulkRetry(Guid[] retryIds, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ManualRetry(Guid id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
