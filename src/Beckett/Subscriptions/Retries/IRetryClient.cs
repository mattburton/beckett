namespace Beckett.Subscriptions.Retries;

public interface IRetryClient
{
    Task RequestManualRetry(Guid id, CancellationToken cancellationToken);

    Task DeleteRetry(Guid id, CancellationToken cancellationToken);
}
