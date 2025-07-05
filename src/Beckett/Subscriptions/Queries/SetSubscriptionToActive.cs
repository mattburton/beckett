using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class SetSubscriptionToActive(
    string groupName,
    string name
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH delete_initialization_checkpoint AS (
                DELETE FROM beckett.checkpoints
                WHERE group_name = $1
                AND name = $2
                AND stream_name = '$initializing'
            )
            UPDATE beckett.subscriptions
            SET status = 'active',
                replay_target_position = NULL
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(SetSubscriptionToActive), sql, out var prepare);

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
