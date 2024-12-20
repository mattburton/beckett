using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;
using Beckett.Dashboard.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Dashboard;

public static class ServiceCollectionExtensions
{
    public static void AddDashboardSupport(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardMessageStore, DashboardMessageStore>();
        services.AddSingleton<IDashboardMetrics, DashboardMetrics>();
        services.AddSingleton<IDashboardSubscriptions, DashboardSubscriptions>();
        services.AddSingleton<IDashboard, DefaultDashboard>();
    }
}
