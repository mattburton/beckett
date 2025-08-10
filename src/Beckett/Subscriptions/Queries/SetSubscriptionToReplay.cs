using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class SetSubscriptionToReplay(
    long subscriptionId
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH delete_initialization_checkpoint AS (
                DELETE FROM beckett.checkpoints
                WHERE subscription_id = $1
                AND stream_name = '$initializing'
            )
            UPDATE beckett.subscriptions
            SET status = 'replay'
            WHERE id = $1;
        """;

        command.CommandText = Query.Build(nameof(SetSubscriptionToReplay), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
