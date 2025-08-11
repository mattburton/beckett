using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecoverExpiredCheckpointReservations(
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
                WHERE cr.reserved_until <= now()
                FOR UPDATE OF cr SKIP LOCKED
                LIMIT $1
            ),
            deleted_reservations AS (
                DELETE FROM beckett.checkpoints_reserved cr
                WHERE cr.id IN (SELECT id FROM expired_reservations)
                RETURNING id
            ),
            updated_checkpoints AS (
                UPDATE beckett.checkpoints c
                SET updated_at = now()
                FROM expired_reservations er
                WHERE c.id = er.id
                RETURNING c.id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name)
                SELECT er.id, now(), sg.name
                FROM expired_reservations er
                INNER JOIN beckett.checkpoints c ON er.id = c.id
                INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE er.status = 'active' AND er.stream_version > er.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = now()
                RETURNING id
            )
            SELECT count(*) FROM inserted_ready;
        """;

        command.CommandText = Query.Build(nameof(RecoverExpiredCheckpointReservations), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = batchSize;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
