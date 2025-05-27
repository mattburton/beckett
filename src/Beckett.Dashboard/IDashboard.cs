namespace Beckett.Dashboard;

public interface IDashboard
{
    IDashboardSubscriptions Subscriptions { get; }
}

public class DefaultDashboard(
    IDashboardSubscriptions subscriptions
) : IDashboard
{
    public IDashboardSubscriptions Subscriptions => subscriptions;
}
