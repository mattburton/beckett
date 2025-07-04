using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateCheckpointPosition(
    long id,
    long streamPosition,
    DateTimeOffset? processAt,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            UPDATE {options.Schema}.checkpoints
            SET stream_position = $2,
                process_at = $3,
                reserved_until = NULL,
                status = 'active',
                retries = NULL
            WHERE id = $1;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz, IsNullable = true });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;
        command.Parameters[1].Value = streamPosition;
        command.Parameters[2].Value = processAt.HasValue ? processAt.Value : DBNull.Value;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
