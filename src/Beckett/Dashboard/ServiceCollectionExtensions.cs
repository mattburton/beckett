using Beckett.Dashboard.Postgres.MessageStore;
using Beckett.Dashboard.Postgres.Metrics;
using Beckett.Dashboard.Postgres.Subscriptions;
using Beckett.Dashboard.Postgres.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Dashboard;

public static class ServiceCollectionExtensions
{
    public static void AddDashboardSupport(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardMessageStore, PostgresDashboardMessageStore>();
        services.AddSingleton<IDashboardMetrics, PostgresDashboardMetrics>();
        services.AddSingleton<IDashboardSubscriptions, PostgresDashboardSubscriptions>();
        services.AddSingleton<IDashboard, DefaultDashboard>();

        services.AddSingleton<ITenantMaterializedViewManager, TenantMaterializedViewManager>();
        services.AddHostedService<RefreshTenantMaterializedView>();
    }
}
