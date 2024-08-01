using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class RecoverExpiredCheckpointReservations(
    TimeSpan reservationTimeout,
    int batchSize
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            UPDATE {schema}.checkpoints c
            SET status = COALESCE(previous_status, 'active'),
                previous_status = NULL,
                reserved_until = NULL
            FROM (
                SELECT c.group_name, c.name, c.stream_name
                FROM {schema}.checkpoints c
                INNER JOIN {schema}.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
                WHERE c.status = 'reserved'
                AND c.reserved_until <= (now() - $1)
                FOR UPDATE SKIP LOCKED
                LIMIT $2
            ) as d
            WHERE c.group_name = d.group_name
            AND c.name = d.name
            AND c.stream_name = d.stream_name;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = reservationTimeout;
        command.Parameters[1].Value = batchSize;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
