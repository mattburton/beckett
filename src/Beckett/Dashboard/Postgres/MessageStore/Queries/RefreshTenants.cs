using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Postgres.MessageStore.Queries;

public class RefreshTenants(PostgresOptions options) : IPostgresDatabaseQuery<int>
{
    public Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"REFRESH MATERIALIZED VIEW CONCURRENTLY {options.Schema}.tenants;";

        command.CommandTimeout = 120;

        return command.ExecuteNonQueryAsync(cancellationToken);
    }
}
