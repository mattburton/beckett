namespace Beckett.Dashboard.Metrics;

public interface IDashboardMetrics
{
    Task<GetSubscriptionMetricsResult> GetSubscriptionMetrics(CancellationToken cancellationToken);
}
