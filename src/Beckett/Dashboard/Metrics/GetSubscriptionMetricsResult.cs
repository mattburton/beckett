namespace Beckett.Dashboard.Metrics;

public record GetSubscriptionMetricsResult(long Lagging, long Retries, long Failed);
