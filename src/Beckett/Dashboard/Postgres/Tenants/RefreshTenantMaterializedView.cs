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
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                const string key = "Beckett:RefreshTenantMaterializedView";

                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(cancellationToken);

                var locked = await database.Execute(new TryAdvisoryLock(key, options), connection, cancellationToken);

                if (locked)
                {
                    await database.Execute(new Query(options), connection, cancellationToken);

                    await database.Execute(new AdvisoryUnlock(key, options), connection, cancellationToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationToken)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing tenant materialized view");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
