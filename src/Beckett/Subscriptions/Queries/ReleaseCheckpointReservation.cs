using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class ReleaseCheckpointReservation(
    long id
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH reservation_info AS (
                SELECT id, target_stream_version 
                FROM beckett.checkpoints_reserved 
                WHERE id = $1
            ),
            checkpoint_info AS (
                SELECT id, stream_position, status, subscription_id
                FROM beckett.checkpoints
                WHERE id = $1
            ),
            deleted_reservation AS (
                DELETE FROM beckett.checkpoints_reserved WHERE id = $1
                RETURNING id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
                SELECT ci.id, now(), sg.name, ri.target_stream_version
                FROM checkpoint_info ci
                INNER JOIN beckett.subscriptions s ON ci.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                INNER JOIN reservation_info ri ON ci.id = ri.id
                WHERE ci.status = 'active'
                AND ri.target_stream_version > ci.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = now(),
                        target_stream_version = EXCLUDED.target_stream_version
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', ci.subscription_id::text)
            FROM checkpoint_info ci
            INNER JOIN reservation_info ri ON ci.id = ri.id
            WHERE ci.status = 'active' AND ri.target_stream_version > ci.stream_position;
        """;

        command.CommandText = Query.Build(nameof(ReleaseCheckpointReservation), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
