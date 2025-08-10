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
                    process_at = $3,
                    reserved_until = NULL,
                    status = 'active',
                    retry_attempts = 0,
                    retries = NULL
                WHERE id = $1
                RETURNING id, stream_version, stream_position
            ),
            deleted_reservation AS (
                DELETE FROM beckett.checkpoints_reserved WHERE id = $1
                RETURNING id
            )
            INSERT INTO beckett.checkpoints_ready (id, process_at)
            SELECT uc.id, $3
            FROM updated_checkpoint uc
            WHERE $3 IS NOT NULL AND uc.stream_version > uc.stream_position
            ON CONFLICT DO NOTHING;
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
