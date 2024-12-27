namespace Beckett.Dashboard;

public interface IDashboard
{
    IDashboardMessageStore MessageStore { get; }
    IDashboardMetrics Metrics { get; }
    IDashboardSubscriptions Subscriptions { get; }
}

public class DefaultDashboard(
    IDashboardMessageStore messageStore,
    IDashboardMetrics metrics,
    IDashboardSubscriptions subscriptions
) : IDashboard
{
    public IDashboardMessageStore MessageStore => messageStore;
    public IDashboardMetrics Metrics => metrics;
    public IDashboardSubscriptions Subscriptions => subscriptions;
}
