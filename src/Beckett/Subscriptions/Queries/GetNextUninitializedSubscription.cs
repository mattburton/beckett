using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetNextUninitializedSubscription(
    string groupName,
    PostgresOptions options
) : IPostgresDatabaseQuery<string?>
{
    public async Task<string?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select name from {options.Schema}.get_next_uninitialized_subscription($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return reader.HasRows ? reader.GetFieldValue<string>(0) : null;
    }
}
