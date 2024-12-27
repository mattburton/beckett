using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Dashboard.Postgres.Tenants;

public class RefreshTenantMaterializedView(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
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

                const string key = "Beckett:RefreshTenantMaterializedView";

                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                var locked = await database.Execute(new TryAdvisoryLock(key, options), connection, stoppingToken);

                if (locked)
                {
                    logger.LogTrace("Starting tenant materialized view refresh");

                    await database.Execute(new Query(options), connection, stoppingToken);

                    await database.Execute(new AdvisoryUnlock(key, options), connection, stoppingToken);

                    logger.LogTrace("Tenant materialized view refresh completed");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
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

    private class Query(PostgresOptions options) : IPostgresDatabaseQuery<int>
    {
        public Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
        {
            command.CommandText = $"REFRESH MATERIALIZED VIEW CONCURRENTLY {options.Schema}.tenants;";

            command.CommandTimeout = 120;

            return command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
