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
            updated_checkpoint AS (
                UPDATE beckett.checkpoints
                SET updated_at = now()
                WHERE id = $1
                RETURNING id, stream_position, status, subscription_id
            ),
            deleted_reservation AS (
                DELETE FROM beckett.checkpoints_reserved WHERE id = $1
                RETURNING id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
                SELECT uc.id, now(), sg.name, ri.target_stream_version
                FROM updated_checkpoint uc
                INNER JOIN beckett.subscriptions s ON uc.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                INNER JOIN reservation_info ri ON uc.id = ri.id
                WHERE uc.status = 'active'
                AND ri.target_stream_version > uc.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = now(),
                        target_stream_version = EXCLUDED.target_stream_version
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', uc.subscription_id::text)
            FROM updated_checkpoint uc
            INNER JOIN reservation_info ri ON uc.id = ri.id
            WHERE uc.status = 'active' AND ri.target_stream_version > uc.stream_position;
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
