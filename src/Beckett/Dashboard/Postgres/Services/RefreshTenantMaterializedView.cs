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

        var timer = new PeriodicTimer(options.TenantRefreshInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await dashboard.MessageStore.RefreshTenants(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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
