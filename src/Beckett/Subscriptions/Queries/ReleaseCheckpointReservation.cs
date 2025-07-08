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
            WITH insert_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, group_name, process_at)
                SELECT c.id, c.group_name, now()
                FROM beckett.checkpoints AS c
                ON CONFLICT (id) DO NOTHING
            )
            DELETE FROM beckett.checkpoints_reserved
            WHERE id = $1;
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
