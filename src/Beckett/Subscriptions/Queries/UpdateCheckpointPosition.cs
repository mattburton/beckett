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
            WITH release_reservation AS (
                DELETE FROM beckett.checkpoints_reserved
                WHERE id = $1
            ),
            insert_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, group_name, process_at)
                SELECT c.id, c.group_name, $3
                FROM beckett.checkpoints c
                WHERE c.id = $1
                AND $3 IS NOT NULL
                ON CONFLICT (id) DO NOTHING
            )
            UPDATE beckett.checkpoints
            SET stream_position = $2,
                status = 'active',
                retries = NULL,
                updated_at = now()
            WHERE id = $1;
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
