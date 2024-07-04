namespace Beckett.Dashboard.Metrics;

public interface IDashboardMetrics
{
    Task<long> GetSubscriptionFailedCount(string application, CancellationToken cancellationToken);

    Task<long> GetSubscriptionLag(string application, CancellationToken cancellationToken);

    Task<long> GetSubscriptionRetryCount(string application, CancellationToken cancellationToken);
}
