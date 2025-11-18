using System.Text.Json;
using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public record RecordCheckpointError(
    long Id,
    long StreamPosition,
    CheckpointStatus Status,
    int Attempt,
    JsonDocument Error,
    DateTimeOffset? ProcessAt
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH update_checkpoint AS (
                UPDATE beckett.checkpoints
                SET stream_position = $2,
                    status = $3,
                    retry_attempts = $4,
                    retries = array_append(
                        coalesce(retries, array[]::beckett.retry[]),
                        row($4, $5, now())::beckett.retry
                    ),
                    updated_at = now()
                WHERE id = $1
                RETURNING group_name, name
            )
            INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name, process_at)
            SELECT $1, group_name, name, $6
            FROM update_checkpoint
            WHERE $6 IS NOT NULL
            ON CONFLICT (checkpoint_id) DO UPDATE
                SET process_at = EXCLUDED.process_at;
        """;

        command.CommandText = Query.Build(nameof(RecordCheckpointError), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStatus() });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz, IsNullable = true });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = Id;
        command.Parameters[1].Value = StreamPosition;
        command.Parameters[2].Value = Status;
        command.Parameters[3].Value = Attempt;
        command.Parameters[4].Value = Error;
        command.Parameters[5].Value = ProcessAt.HasValue ? ProcessAt.Value : DBNull.Value;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
