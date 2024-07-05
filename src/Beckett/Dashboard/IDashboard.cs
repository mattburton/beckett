using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;
using Beckett.Dashboard.Subscriptions;

namespace Beckett.Dashboard;

public interface IDashboard
{
    IDashboardMessageStore MessageStore { get; }
    IDashboardMetrics Metrics { get; }
    IDashboardSubscriptions Subscriptions { get; }
}
