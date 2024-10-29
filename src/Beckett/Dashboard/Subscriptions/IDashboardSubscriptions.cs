namespace Beckett.Dashboard.Subscriptions;

public interface IDashboardSubscriptions
{
    Task<GetSubscriptionsResult> GetSubscriptions(int page, int pageSize, CancellationToken cancellationToken);

    Task<GetSubscriptionResult?> GetSubscription(string groupName, string name, CancellationToken cancellationToken);

    Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetReservationsResult> GetReservations(string? query, int page, int pageSize, CancellationToken cancellationToken);

    Task<GetRetriesResult> GetRetries(string? query, int page, int pageSize, CancellationToken cancellationToken);

    Task<GetCheckpointResult?> GetCheckpoint(long id, CancellationToken cancellationToken);

    Task<GetFailedResult> GetFailed(string? query, int page, int pageSize, CancellationToken cancellationToken);
}
