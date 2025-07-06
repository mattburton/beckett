using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class SetSubscriptionToReplay(
    string groupName,
    string name
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH delete_initialization_checkpoint AS (
                DELETE FROM beckett.checkpoints
                WHERE group_name = $1
                AND name = $2
                AND stream_name = '$initializing'
            )
            UPDATE beckett.subscriptions
            SET status = 'replay'
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(SetSubscriptionToReplay), sql, out var prepare);

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
