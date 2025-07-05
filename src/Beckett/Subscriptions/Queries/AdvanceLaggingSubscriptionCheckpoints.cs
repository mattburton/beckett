using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class AdvanceLaggingSubscriptionCheckpoints(string groupName, string name) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.checkpoints
            SET stream_position = stream_version,
                process_at = null
            WHERE id IN (
                SELECT id
                FROM beckett.checkpoints
                WHERE group_name = $1
                AND name = $2
                AND lagging = true
                LIMIT 500
            );
        """;

        command.CommandText = Query.Build(nameof(AdvanceLaggingSubscriptionCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
