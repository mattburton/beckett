namespace Beckett.Dashboard.Subscriptions;

public interface IDashboardSubscriptions
{
    Task<GetSubscriptionsResult> GetSubscriptions(int page, int pageSize, CancellationToken cancellationToken);

    Task<GetSubscriptionResult?> GetSubscription(string groupName, string name, CancellationToken cancellationToken);

    Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(int page, int pageSize, CancellationToken cancellationToken);

    Task<GetRetriesResult> GetRetries(int page, int pageSize, CancellationToken cancellationToken);

    Task<GetCheckpointResult?> GetCheckpoint(long id, CancellationToken cancellationToken);

    Task<GetFailedResult> GetFailed(int page, int pageSize, CancellationToken cancellationToken);
}
