namespace Beckett.Dashboard.Subscriptions;

public interface IDashboardSubscriptions
{
    Task<GetSubscriptionsResult> GetSubscriptions(CancellationToken cancellationToken);

    Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(CancellationToken cancellationToken);

    Task<GetRetriesResult> GetRetries(CancellationToken cancellationToken);

    Task<GetRetryDetailsResult?> GetRetryDetails(Guid id, CancellationToken cancellationToken);

    Task<GetFailedResult> GetFailed(CancellationToken cancellationToken);
}
