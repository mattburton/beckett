using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;
using Beckett.Dashboard.Subscriptions;

namespace Beckett.Dashboard;

public class DefaultDashboard(
    IDashboardMessageStore messageStore,
    IDashboardMetrics metrics,
    IDashboardSubscriptions subscriptions) : IDashboard
{
    public IDashboardMessageStore MessageStore => messageStore;
    public IDashboardMetrics Metrics => metrics;
    public IDashboardSubscriptions Subscriptions => subscriptions;
}
