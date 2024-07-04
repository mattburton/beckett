using Beckett.Dashboard.Metrics.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Metrics;

public class DashboardMetrics(IPostgresDatabase database) : IDashboardMetrics
{
    public Task<long> GetSubscriptionFailedCount(string application, CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionFailedCount(application), cancellationToken);
    }

    public Task<long> GetSubscriptionLag(string application, CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionLag(application), cancellationToken);
    }

    public Task<long> GetSubscriptionRetryCount(string application, CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionFailedCount(application), cancellationToken);
    }
}
