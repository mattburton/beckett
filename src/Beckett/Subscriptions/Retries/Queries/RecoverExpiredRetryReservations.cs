using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Retries.Queries;

public class RecoverExpiredRetryReservations(
    TimeSpan reservationTimeout,
    int batchSize
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            UPDATE {schema}.retries r
            SET status = previous_status,
                previous_status = NULL,
                reserved_until = NULL
            FROM (
                SELECT r2.id
                FROM {schema}.retries r2
                INNER JOIN {schema}.subscriptions s ON r2.group_name = s.group_name AND r2.name = s.name
                WHERE r2.status = 'reserved'
                AND r2.previous_status IS NOT NULL
                AND r2.reserved_until <= (now() - $1)
                FOR UPDATE SKIP LOCKED
                LIMIT $2
            ) as d
            WHERE r.id = d.id;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = reservationTimeout;
        command.Parameters[1].Value = batchSize;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
