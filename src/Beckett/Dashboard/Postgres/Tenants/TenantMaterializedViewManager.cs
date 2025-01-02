using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Dashboard.Postgres.Tenants;

public class TenantMaterializedViewManager(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    PostgresOptions options,
    ILogger<TenantMaterializedViewManager> logger
) : ITenantMaterializedViewManager
{
    public async Task Refresh(CancellationToken cancellationToken)
    {
        const string key = "Beckett:RefreshTenantMaterializedView";

        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var locked = await database.Execute(new TryAdvisoryLock(key, options), connection, cancellationToken);

        if (!locked)
        {
            return;
        }

        logger.LogTrace("Starting tenant materialized view refresh");

        await database.Execute(new Query(options), connection, cancellationToken);

        await database.Execute(new AdvisoryUnlock(key, options), connection, cancellationToken);

        logger.LogTrace("Tenant materialized view refresh completed");
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
