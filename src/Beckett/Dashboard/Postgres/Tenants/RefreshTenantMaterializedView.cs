using Beckett.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Dashboard.Postgres.Tenants;

public class RefreshTenantMaterializedView(
    ITenantMaterializedViewManager tenantMaterializedViewManager,
    PostgresOptions options,
    ILogger<RefreshTenantMaterializedView> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (true)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                await tenantMaterializedViewManager.Refresh(stoppingToken);

                await Task.Delay(options.TenantRefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == stoppingToken)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing tenant materialized view");
            }
        }
    }
}
