using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class GetNextUninitializedSubscription(string groupName) : IPostgresDatabaseQuery<string?>
{
    public async Task<string?> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $"select name from {schema}.get_next_uninitialized_subscription($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = groupName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return reader.HasRows ? reader.GetFieldValue<string>(0) : null;
    }
}
