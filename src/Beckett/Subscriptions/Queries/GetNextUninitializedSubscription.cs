using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetNextUninitializedSubscription(
    int groupId,
    PostgresOptions options
) : IPostgresDatabaseQuery<int?>
{
    public async Task<int?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select id from {options.Schema}.get_next_uninitialized_subscription($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return reader.HasRows ? reader.GetFieldValue<int>(0) : null;
    }
}
