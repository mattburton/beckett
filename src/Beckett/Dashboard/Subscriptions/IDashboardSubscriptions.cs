namespace Beckett.Dashboard.Subscriptions;

public interface IDashboardSubscriptions
{
    Task<GetSubscriptionsResult> GetSubscriptions(CancellationToken cancellationToken);

    Task<GetRetriesResult> GetRetries(CancellationToken cancellationToken);

    Task<GetFailedResult> GetFailed(CancellationToken cancellationToken);
}
