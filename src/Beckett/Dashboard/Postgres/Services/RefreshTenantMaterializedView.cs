using Beckett.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Dashboard.Postgres.Services;

public class RefreshTenantMaterializedView(
    IDashboard dashboard,
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

                await dashboard.MessageStore.RefreshTenants(stoppingToken);

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
