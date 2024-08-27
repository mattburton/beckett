namespace Beckett.Subscriptions.Retries;

public interface IRetryProcessor
{
    Task ManualRetry(RetryState retry, CancellationToken cancellationToken);

    Task Retry(RetryState retry, CancellationToken cancellationToken);
}
