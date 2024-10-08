using Beckett.Dashboard.Metrics.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Metrics;

public class DashboardMetrics(IPostgresDatabase database, PostgresOptions options) : IDashboardMetrics
{
    public Task<long> GetSubscriptionFailedCount(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionFailedCount(options), cancellationToken);
    }

    public Task<long> GetSubscriptionLag(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionLagCount(options), cancellationToken);
    }

    public Task<long> GetSubscriptionRetryCount(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionRetryCount(options), cancellationToken);
    }
}
