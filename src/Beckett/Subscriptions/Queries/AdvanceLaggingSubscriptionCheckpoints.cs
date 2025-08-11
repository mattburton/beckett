using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class AdvanceLaggingSubscriptionCheckpoints(long subscriptionId) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH updated_checkpoints AS (
                UPDATE beckett.checkpoints c
                SET stream_position = cr.target_stream_version,
                    updated_at = now()
                FROM beckett.checkpoints_ready cr
                WHERE c.id = cr.id
                AND c.subscription_id = $1
                AND c.id IN (
                    SELECT c2.id
                    FROM beckett.checkpoints c2
                    INNER JOIN beckett.checkpoints_ready cr2 ON c2.id = cr2.id
                    WHERE c2.subscription_id = $1
                    LIMIT 500
                )
                RETURNING id
            )
            DELETE FROM beckett.checkpoints_ready
            WHERE id IN (SELECT id FROM updated_checkpoints);
        """;

        command.CommandText = Query.Build(nameof(AdvanceLaggingSubscriptionCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
