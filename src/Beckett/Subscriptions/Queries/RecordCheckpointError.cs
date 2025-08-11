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
            WITH updated_checkpoint AS (
                UPDATE beckett.checkpoints
                SET stream_position = $2,
                    status = $3,
                    retry_attempts = $4,
                    updated_at = now(),
                    retries = array_append(
                        coalesce(retries, array[]::beckett.retry[]),
                        row($4, $5, now())::beckett.retry
                    )
                WHERE id = $1
                RETURNING id, stream_version, stream_position, status, subscription_id
            ),
            deleted_reservation AS (
                DELETE FROM beckett.checkpoints_reserved WHERE id = $1
                RETURNING id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name)
                SELECT uc.id, $6, sg.name
                FROM updated_checkpoint uc
                INNER JOIN beckett.subscriptions s ON uc.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE $6 IS NOT NULL
                AND uc.status = 'retry'
                ON CONFLICT (id) DO UPDATE
                    SET process_at = EXCLUDED.process_at
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', uc.subscription_id::text)
            FROM updated_checkpoint uc
            WHERE $6 IS NOT NULL;
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
