using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateCheckpointPosition(
    long id,
    long streamPosition,
    DateTimeOffset? processAt
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH delete_reserved AS (
                DELETE FROM beckett.checkpoints_reserved
                WHERE checkpoint_id = $1
                RETURNING group_name
            ),
            update_checkpoint AS (
                UPDATE beckett.checkpoints
                SET stream_position = $3,
                    status = 'active',
                    retry_attempts = 0,
                    retries = NULL,
                    updated_at = now()
                WHERE id = $1
                RETURNING group_name, name
            )
            INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name, process_at)
            SELECT $1, group_name, name, $2
            FROM update_checkpoint
            WHERE $2 IS NOT NULL
            ON CONFLICT (checkpoint_id) DO UPDATE
                SET process_at = EXCLUDED.process_at;
        """;

        command.CommandText = Query.Build(nameof(UpdateCheckpointPosition), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;
        command.Parameters[1].Value = processAt.HasValue ? processAt.Value : DBNull.Value;
        command.Parameters[2].Value = streamPosition;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
