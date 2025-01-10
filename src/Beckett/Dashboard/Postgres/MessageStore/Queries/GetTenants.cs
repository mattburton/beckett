using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Postgres.MessageStore.Queries;

public class GetTenants(PostgresOptions options) : IPostgresDatabaseQuery<GetTenantsResult>
{
    public async Task<GetTenantsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select tenant from {options.Schema}.tenants order by tenant;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetFieldValue<string>(0));
        }

        return new GetTenantsResult(results);
    }
}
