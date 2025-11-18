using Beckett.Database;
using Beckett.Database.Types;
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
            WITH ready AS (
                DELETE FROM beckett.checkpoints_ready
                WHERE checkpoint_id = (
                    SELECT cr.checkpoint_id
                    FROM beckett.checkpoints_ready cr
                    INNER JOIN beckett.subscriptions s ON cr.group_name = s.group_name AND cr.name = s.name
                    WHERE cr.group_name = $1
                    AND cr.process_at <= now()
                    AND s.status = ANY($2)
                    ORDER BY cr.process_at
                    FOR UPDATE OF cr
                    SKIP LOCKED
                    LIMIT 1
                )
                RETURNING checkpoint_id, group_name
            ),
            reserved AS (
                INSERT INTO beckett.checkpoints_reserved (checkpoint_id, group_name, reserved_until)
                SELECT checkpoint_id, group_name, now() + $3
                FROM ready
                ON CONFLICT (checkpoint_id) DO NOTHING
            )
            SELECT
                c.id,
                c.group_name,
                c.name,
                c.stream_name,
                c.stream_position,
                c.stream_version,
                c.retry_attempts,
                c.status,
                s.replay_target_position
            FROM ready r
            INNER JOIN beckett.checkpoints c ON r.checkpoint_id = c.id
            INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name;
        """;

        command.CommandText = Query.Build(nameof(ReserveNextAvailableCheckpoint), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.SubscriptionStatusArray() });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        var statusList = new List<SubscriptionStatus>();

        if (replayMode is ReplayMode.All or ReplayMode.ActiveOnly)
        {
            statusList.Add(SubscriptionStatus.Active);
        }

        if (replayMode is ReplayMode.All or ReplayMode.ReplayOnly)
        {
            statusList.Add(SubscriptionStatus.Replay);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = statusList.ToArray();
        command.Parameters[2].Value = reservationTimeout;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return Checkpoint.From(reader);
    }
}
