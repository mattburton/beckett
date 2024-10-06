using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionLag(PostgresOptions options) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            WITH lagging AS (
                SELECT COUNT(*) AS lagging
                FROM {options.Schema}.subscriptions s
                INNER JOIN {options.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status = 'active'
                AND c.status = 'lagging'
                GROUP BY c.group_name, c.name
            )
            SELECT COUNT(*)
            FROM lagging l;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
