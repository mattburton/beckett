using Beckett.Dashboard.Postgres.Metrics;
using Beckett.Dashboard.Postgres.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Dashboard;

public static class ServiceCollectionExtensions
{
    internal static void AddDashboardSupport(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardMetrics, PostgresDashboardMetrics>();
        services.AddSingleton<IDashboardSubscriptions, PostgresDashboardSubscriptions>();
        services.AddSingleton<IDashboard, DefaultDashboard>();
    }
}
