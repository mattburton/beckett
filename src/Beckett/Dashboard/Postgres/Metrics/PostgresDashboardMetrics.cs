using Beckett.Dashboard.Postgres.Metrics.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Postgres.Metrics;

public class PostgresDashboardMetrics(IPostgresDatabase database, PostgresOptions options) : IDashboardMetrics
{
    public Task<GetSubscriptionMetricsResult> GetSubscriptionMetrics(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptionMetrics(options), cancellationToken);
    }
}
