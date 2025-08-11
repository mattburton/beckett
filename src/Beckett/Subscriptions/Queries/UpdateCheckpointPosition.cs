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
            WITH updated_checkpoint AS (
                UPDATE beckett.checkpoints
                SET stream_position = $2,
                    status = 'active',
                    retry_attempts = 0,
                    retries = NULL,
                    updated_at = now()
                WHERE id = $1
                RETURNING id, stream_position, subscription_id
            ),
            deleted_reservation AS (
                DELETE FROM beckett.checkpoints_reserved WHERE id = $1
                RETURNING id
            ),
            removed_ready AS (
                DELETE FROM beckett.checkpoints_ready 
                WHERE id = $1
                RETURNING id
            )
            SELECT count(*) FROM removed_ready;
        """;

        command.CommandText = Query.Build(nameof(UpdateCheckpointPosition), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz, IsNullable = true });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;
        command.Parameters[1].Value = streamPosition;
        command.Parameters[2].Value = processAt.HasValue ? processAt.Value : DBNull.Value;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
