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
            WITH reserved_checkpoint AS (
                DELETE FROM beckett.checkpoints_ready cr
                WHERE cr.id IN (
                    SELECT cr2.id
                    FROM beckett.checkpoints_ready cr2
                    INNER JOIN beckett.checkpoints c ON cr2.id = c.id
                    INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
                    WHERE cr2.subscription_group_name = $1
                    AND cr2.process_at <= now()
                    AND ($3 = false OR (s.status = 'active' OR s.status = 'replay'))
                    AND ($4 = false OR s.status = 'active')
                    AND ($5 = false OR s.status = 'replay')
                    AND NOT EXISTS (SELECT 1 FROM beckett.checkpoints_reserved cres WHERE cres.id = c.id)
                    ORDER BY cr2.process_at, cr2.id
                    LIMIT 1
                    FOR UPDATE
                    SKIP LOCKED
                )
                RETURNING id
            ),
            updated_checkpoint AS (
                UPDATE beckett.checkpoints c
                SET updated_at = now()
                FROM reserved_checkpoint rc
                WHERE c.id = rc.id
                RETURNING c.id, c.subscription_id, c.stream_name, c.stream_position, 
                         c.stream_version, c.retry_attempts, c.status
            ),
            inserted_reservation AS (
                INSERT INTO beckett.checkpoints_reserved (id, reserved_until)
                SELECT uc.id, now() + $2
                FROM updated_checkpoint uc
                RETURNING id
            )
            SELECT 
                uc.id,
                uc.subscription_id,
                uc.stream_name,
                uc.stream_position,
                uc.stream_version,
                uc.retry_attempts,
                uc.status,
                s.replay_target_position
            FROM updated_checkpoint uc
            INNER JOIN beckett.subscriptions s ON uc.subscription_id = s.id
            INNER JOIN inserted_reservation ir ON uc.id = ir.id;
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
