using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class ReserveNextAvailableCheckpoint(
    string groupName,
    TimeSpan reservationTimeout,
    ReplayMode replayMode
) : IPostgresDatabaseQuery<Checkpoint?>
{
    public async Task<Checkpoint?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH reserved AS (
                DELETE FROM beckett.checkpoints_ready cr
                WHERE cr.checkpoint_id = (
                    SELECT cr.checkpoint_id
                    FROM beckett.checkpoints_ready cr
                    INNER JOIN beckett.checkpoints c ON cr.checkpoint_id = c.id
                    INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
                    WHERE cr.group_name = $1
                    AND cr.process_at <= now()
                    AND ($3 = false OR s.status IN ('active', 'replay'))
                    AND ($4 = false OR s.status = 'active')
                    AND ($5 = false OR s.status = 'replay')
                    ORDER BY cr.process_at
                    LIMIT 1
                    FOR UPDATE OF cr
                    SKIP LOCKED
                )
                RETURNING cr.checkpoint_id
            )
            UPDATE beckett.checkpoints c
            SET reserved_until = now() + $2
            FROM reserved r
            INNER JOIN beckett.checkpoints c2 ON r.checkpoint_id = c2.id
            INNER JOIN beckett.subscriptions s ON c2.group_name = s.group_name AND c2.name = s.name
            WHERE c.id = r.checkpoint_id
            RETURNING
                c.id,
                c.group_name,
                c.name,
                c.stream_name,
                c.stream_position,
                c.stream_version,
                c.retry_attempts,
                c.status,
                s.replay_target_position;
        """;

        command.CommandText = Query.Build(nameof(ReserveNextAvailableCheckpoint), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = reservationTimeout;
        command.Parameters[2].Value = replayMode == ReplayMode.All;
        command.Parameters[3].Value = replayMode == ReplayMode.ActiveOnly;
        command.Parameters[4].Value = replayMode == ReplayMode.ReplayOnly;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return Checkpoint.From(reader);
    }
}
