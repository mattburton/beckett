using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.MessageStore.Components.Queries;

public class TenantsQuery(PostgresOptions options) : IPostgresDatabaseQuery<TenantsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select tenant from {options.Schema}.tenants order by tenant;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetFieldValue<string>(0));
        }

        return new Result(results);
    }

    public record Result(List<string> Tenants);
}
