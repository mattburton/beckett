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
            WITH delete_reserved AS (
                DELETE FROM beckett.checkpoints_reserved
                WHERE checkpoint_id = $1
            )
            INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name, process_at)
            SELECT id, group_name, name, now()
            FROM beckett.checkpoints
            WHERE id = $1
            ON CONFLICT (checkpoint_id) DO UPDATE
                SET process_at = EXCLUDED.process_at;
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
