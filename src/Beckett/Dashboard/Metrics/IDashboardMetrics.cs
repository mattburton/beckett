namespace Beckett.Dashboard.Metrics;

public interface IDashboardMetrics
{
    Task<long> GetSubscriptionFailedCount(CancellationToken cancellationToken);

    Task<long> GetSubscriptionLag(CancellationToken cancellationToken);

    Task<long> GetSubscriptionRetryCount(CancellationToken cancellationToken);
}
