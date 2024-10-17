using Beckett.Dashboard.Subscriptions.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions;

public class DashboardSubscriptions(
    IPostgresDatabase database,
    PostgresOptions options
) : IDashboardSubscriptions
{
    public Task<GetSubscriptionsResult> GetSubscriptions(int page, int pageSize, CancellationToken cancellationToken)
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetSubscriptions(offset, pageSize, options), cancellationToken);
    }

    public Task<GetSubscriptionResult?> GetSubscription(
        string groupName,
        string name,
        CancellationToken cancellationToken
    )
    {
        return database.Execute(new GetSubscription(groupName, name, options), cancellationToken);
    }

    public Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetLaggingSubscriptions(offset, pageSize, options), cancellationToken);
    }

    public Task<GetRetriesResult> GetRetries(int page, int pageSize, CancellationToken cancellationToken)
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetRetries(offset, pageSize, options), cancellationToken);
    }

    public Task<GetCheckpointResult?> GetCheckpoint(long id, CancellationToken cancellationToken)
    {
        return database.Execute(new GetCheckpoint(id, options), cancellationToken);
    }

    public Task<GetFailedResult> GetFailed(int page, int pageSize, CancellationToken cancellationToken)
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetFailed(offset, pageSize, options), cancellationToken);
    }
}
