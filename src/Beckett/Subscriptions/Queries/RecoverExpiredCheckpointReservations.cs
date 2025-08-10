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
            UPDATE beckett.checkpoints c
            SET reserved_until = NULL
            FROM (
                SELECT c2.id
                FROM beckett.checkpoints c2
                INNER JOIN beckett.subscriptions s ON c2.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE sg.name = $1
                AND c2.reserved_until <= now()
                FOR UPDATE SKIP LOCKED
                LIMIT $2
            ) as d
            WHERE c.id = d.id;
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
