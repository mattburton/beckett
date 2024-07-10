using Beckett.Dashboard.Metrics.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Metrics;

public class DashboardMetrics(IPostgresDatabase database) : IDashboardMetrics
{
    public Task<long> GetSubscriptionFailedCount(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionFailedCount(), cancellationToken);
    }

    public Task<long> GetSubscriptionLag(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionLag(), cancellationToken);
    }

    public Task<long> GetSubscriptionRetryCount(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionRetryCount(), cancellationToken);
    }
}
