using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetNextUninitializedSubscription(string groupName) : IPostgresDatabaseQuery<string?>
{
    public async Task<string?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name
            FROM beckett.subscriptions
            WHERE group_name = $1
            AND status in ('uninitialized', 'backfill')
            LIMIT 1;
        """;

        command.CommandText = Query.Build(nameof(GetNextUninitializedSubscription), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return reader.HasRows ? reader.GetFieldValue<string>(0) : null;
    }
}
