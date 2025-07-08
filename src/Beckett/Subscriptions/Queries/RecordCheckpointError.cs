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
            WITH release_reservation AS (
                DELETE FROM beckett.checkpoints_reserved
                WHERE id = $1
            ),
            insert_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, group_name, process_at)
                SELECT c.id, c.group_name, $6
                FROM beckett.checkpoints AS c
                WHERE c.id = $1
                AND $6 IS NOT NULL
                ON CONFLICT (id) DO NOTHING
            )
            UPDATE beckett.checkpoints
            SET stream_position = $2,
                status = $3,
                retries = array_append(
                    coalesce(retries, array[]::beckett.retry[]),
                    row($4, $5, now())::beckett.retry
                ),
                updated_at = now()
            WHERE id = $1;
        """;

        command.CommandText = Query.Build(nameof(RecordCheckpointError), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStatus("beckett") });
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
