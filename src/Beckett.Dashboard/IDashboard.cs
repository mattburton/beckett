namespace Beckett.Dashboard;

public interface IDashboard
{
    IDashboardMetrics Metrics { get; }
    IDashboardSubscriptions Subscriptions { get; }
}

public class DefaultDashboard(
    IDashboardMetrics metrics,
    IDashboardSubscriptions subscriptions
) : IDashboard
{
    public IDashboardMetrics Metrics => metrics;
    public IDashboardSubscriptions Subscriptions => subscriptions;
}
