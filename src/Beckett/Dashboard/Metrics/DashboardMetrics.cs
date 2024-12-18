using Beckett.Dashboard.Metrics.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Metrics;

public class DashboardMetrics(IPostgresDatabase database, PostgresOptions options) : IDashboardMetrics
{
    public Task<GetSubscriptionMetricsResult> GetSubscriptionMetrics(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionMetrics(options), cancellationToken);
    }
}
