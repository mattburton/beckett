using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Dashboard;

public static class ServiceCollectionExtensions
{
    public static void AddDashboard(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardMessageStore, DashboardMessageStore>();

        services.AddSingleton<IDashboardMetrics, DashboardMetrics>();

        services.AddSingleton<IDashboard, Dashboard>();
    }
}
