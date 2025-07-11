using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateSubscriptionReplayTargetPosition(
    string groupName,
    string name,
    long position
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.subscriptions
            SET replay_target_position = $3
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(UpdateSubscriptionReplayTargetPosition), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = position;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
