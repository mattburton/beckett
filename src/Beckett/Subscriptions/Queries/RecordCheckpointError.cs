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
    DateTimeOffset? ProcessAt,
    PostgresOptions Options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            UPDATE {Options.Schema}.checkpoints
            SET stream_position = $2,
            process_at = $6,
            reserved_until = NULL,
            status = $3,
            retries = array_append(
                coalesce(retries, array[]::{Options.Schema}.retry[]),
                row($4, $5, now())::{Options.Schema}.retry
            )
            WHERE id = $1;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStatus(Options.Schema) });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz, IsNullable = true });

        if (Options.PrepareStatements)
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
