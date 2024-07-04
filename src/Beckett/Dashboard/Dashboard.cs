using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;

namespace Beckett.Dashboard;

public class Dashboard(IDashboardMessageStore messageStore, IDashboardMetrics metrics) : IDashboard
{
    public IDashboardMessageStore MessageStore => messageStore;
    public IDashboardMetrics Metrics => metrics;
}
