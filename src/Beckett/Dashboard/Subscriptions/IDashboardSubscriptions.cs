namespace Beckett.Dashboard.Subscriptions;

public interface IDashboardSubscriptions
{
    Task<GetSubscriptionsResult> GetSubscriptions(CancellationToken cancellationToken);

    Task<GetSubscriptionResult?> GetSubscription(string groupName, string name, CancellationToken cancellationToken);

    Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(CancellationToken cancellationToken);

    Task<GetRetriesResult> GetRetries(CancellationToken cancellationToken);

    Task<GetCheckpointResult?> GetCheckpoint(long id, CancellationToken cancellationToken);

    Task<GetFailedResult> GetFailed(CancellationToken cancellationToken);
}
