namespace Beckett.Subscriptions.Retries;

public interface IRetryClient
{
    Task ManualRetry(Guid id, CancellationToken cancellationToken);

    Task DeleteRetry(Guid id, CancellationToken cancellationToken);
}
