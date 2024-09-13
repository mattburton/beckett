using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionLag : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            WITH lagging AS (
                SELECT COUNT(*) AS lagging
                FROM {schema}.subscriptions s
                INNER JOIN {schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status = 'active'
                AND c.status = 'lagging'
                GROUP BY c.group_name, c.name
            )
            SELECT COUNT(*)
            FROM lagging l;
        ";

        await command.PrepareAsync(cancellationToken);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
