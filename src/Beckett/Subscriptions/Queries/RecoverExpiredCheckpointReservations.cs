using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecoverExpiredCheckpointReservations(
    string groupName,
    int batchSize
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH expired_reservations AS (
                SELECT cr.id, c.stream_version, c.stream_position, c.status
                FROM beckett.checkpoints_reserved cr
                INNER JOIN beckett.checkpoints c ON cr.id = c.id
                INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE sg.name = $1
                AND cr.reserved_until <= now()
                FOR UPDATE OF cr SKIP LOCKED
                LIMIT $2
            ),
            deleted_reservations AS (
                DELETE FROM beckett.checkpoints_reserved cr
                WHERE cr.id IN (SELECT id FROM expired_reservations)
                RETURNING id
            ),
            updated_checkpoints AS (
                UPDATE beckett.checkpoints c
                SET reserved_until = NULL
                FROM expired_reservations er
                WHERE c.id = er.id
                RETURNING c.id
            )
            INSERT INTO beckett.checkpoints_ready (id, process_at)
            SELECT er.id, now()
            FROM expired_reservations er
            WHERE er.status = 'active' AND er.stream_version > er.stream_position
            ON CONFLICT DO NOTHING;
        """;

        command.CommandText = Query.Build(nameof(RecoverExpiredCheckpointReservations), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = batchSize;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
