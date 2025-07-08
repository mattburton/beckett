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
            WITH reservation_candidate AS (
                SELECT c.id,
                       c.group_name,
                       c.name,
                       c.stream_name,
                       c.stream_position,
                       c.retry_attempts,
                       c.status,
                       s.replay_target_position
                FROM beckett.checkpoints_ready r
                INNER JOIN beckett.checkpoints c ON r.id = c.id
                INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
                WHERE c.group_name = $1
                AND ($3 = false OR (s.status = 'active' OR s.status = 'replay'))
                AND ($4 = false OR s.status = 'active')
                AND ($5 = false OR s.status = 'replay')
                ORDER BY r.process_at
                LIMIT 1
                FOR UPDATE
                SKIP LOCKED
            ),
            reserve_checkpoint AS (
                INSERT INTO beckett.checkpoints_reserved (id, group_name, reserved_until)
                SELECT r.id, r.group_name, now() + $2
                FROM reservation_candidate r
                ON CONFLICT (id) DO NOTHING
                RETURNING id
            ),
            reserved_checkpoint_id AS (
                SELECT id
                FROM reserve_checkpoint
                UNION ALL
                SELECT NULL
                LIMIT 1
            ),
            delete_ready AS (
                DELETE FROM beckett.checkpoints_ready r
                USING reserved_checkpoint_id i
                WHERE r.id = i.id
                AND i.id IS NOT NULL
            )
            SELECT r.id,
                   r.group_name,
                   r.name,
                   r.stream_name,
                   r.stream_position,
                   r.retry_attempts,
                   r.status,
                   r.replay_target_position
            FROM reservation_candidate r
            INNER JOIN reserved_checkpoint_id i on r.id = i.id;
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
