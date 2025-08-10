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
            WITH updated_checkpoint AS (
                UPDATE beckett.checkpoints
                SET process_at = NULL,
                    reserved_until = NULL,
                    updated_at = now()
                WHERE id = $1
                RETURNING id, stream_version, stream_position, status, subscription_id
            ),
            deleted_reservation AS (
                DELETE FROM beckett.checkpoints_reserved WHERE id = $1
                RETURNING id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at)
                SELECT uc.id, now()
                FROM updated_checkpoint uc
                WHERE uc.status = 'active' AND uc.stream_version > uc.stream_position
                ON CONFLICT (id) DO UPDATE SET process_at = now()
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', uc.subscription_id::text)
            FROM updated_checkpoint uc
            WHERE EXISTS (SELECT 1 FROM inserted_ready ir WHERE ir.id = uc.id);
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
