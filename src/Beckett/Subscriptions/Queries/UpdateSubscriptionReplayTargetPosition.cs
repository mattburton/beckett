using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateSubscriptionReplayTargetPosition(
    long subscriptionId,
    long position
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.subscriptions
            SET replay_target_position = $2
            WHERE id = $1;
        """;

        command.CommandText = Query.Build(nameof(UpdateSubscriptionReplayTargetPosition), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;
        command.Parameters[1].Value = position;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
