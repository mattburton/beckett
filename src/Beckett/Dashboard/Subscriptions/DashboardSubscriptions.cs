using Beckett.Dashboard.Subscriptions.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions;

public class DashboardSubscriptions(
    IPostgresDatabase database,
    PostgresOptions options
) : IDashboardSubscriptions
{
    public Task<GetSubscriptionsResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptions(options), cancellationToken);
    }

    public Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(CancellationToken cancellationToken)
    {
        return database.Execute(new GetLaggingSubscriptions(options), cancellationToken);
    }

    public Task<GetRetriesResult> GetRetries(CancellationToken cancellationToken)
    {
        return database.Execute(new GetRetries(options), cancellationToken);
    }

    public Task<GetCheckpointResult?> GetCheckpointDetails(long id, CancellationToken cancellationToken)
    {
        return database.Execute(new GetCheckpoint(id, options), cancellationToken);
    }

    public Task<GetFailedResult> GetFailed(CancellationToken cancellationToken)
    {
        return database.Execute(new GetFailed(options), cancellationToken);
    }
}
