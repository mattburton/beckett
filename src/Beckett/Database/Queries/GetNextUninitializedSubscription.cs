using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class GetNextUninitializedSubscription : IPostgresDatabaseQuery<string?>
{
    public async Task<string?> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $"select name from {schema}.get_next_uninitialized_subscription();";

        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return reader.HasRows ? reader.GetFieldValue<string>(0) : null;
    }
}
