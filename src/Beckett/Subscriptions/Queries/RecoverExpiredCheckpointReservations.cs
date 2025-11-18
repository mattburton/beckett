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
                DELETE FROM beckett.checkpoints_reserved
                WHERE group_name = $1
                AND reserved_until <= now()
                RETURNING checkpoint_id, group_name
            )
            INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name, process_at)
            SELECT c.id, c.group_name, c.name, now()
            FROM expired_reservations er
            INNER JOIN beckett.checkpoints c ON er.checkpoint_id = c.id
            ON CONFLICT (checkpoint_id) DO UPDATE
                SET process_at = EXCLUDED.process_at;
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
