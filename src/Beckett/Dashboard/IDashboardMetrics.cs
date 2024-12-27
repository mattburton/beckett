namespace Beckett.Dashboard;

public interface IDashboardMetrics
{
    Task<GetSubscriptionMetricsResult> GetSubscriptionMetrics(CancellationToken cancellationToken);
}

public record GetSubscriptionMetricsResult(long Lagging, long Retries, long Failed);
