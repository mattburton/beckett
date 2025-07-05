using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.MessageStore.Components.Queries;

public class TenantsQuery : IPostgresDatabaseQuery<TenantsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = "SELECT tenant FROM beckett.tenants ORDER BY tenant;";

        command.CommandText = Query.Build(nameof(TenantsQuery), sql, out var prepare);

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

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
