using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;

namespace Beckett.Dashboard;

public interface IDashboard
{
    IDashboardMessageStore MessageStore { get; }
    IDashboardMetrics Metrics { get; }
}
