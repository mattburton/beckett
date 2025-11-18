using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.BulkSkip;

public class BulkSkipQuery(
    long[] ids
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH delete_ready AS (
                DELETE FROM beckett.checkpoints_ready
                WHERE checkpoint_id = ANY($1)
            ),
            delete_reserved AS (
                DELETE FROM beckett.checkpoints_reserved
                WHERE checkpoint_id = ANY($1)
            ),
            update_checkpoints AS (
                UPDATE beckett.checkpoints
                SET status = 'active',
                    retry_attempts = 0,
                    retries = NULL,
                    updated_at = now()
                WHERE id = ANY($1)
                RETURNING id, group_name, name
            )
            INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name)
            SELECT id, group_name, name
            FROM update_checkpoints
            ON CONFLICT (checkpoint_id) DO UPDATE SET
                process_at = EXCLUDED.process_at;
        """;

        command.CommandText = Query.Build(nameof(BulkSkipQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = ids;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
