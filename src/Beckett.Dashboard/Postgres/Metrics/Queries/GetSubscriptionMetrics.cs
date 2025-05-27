using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Postgres.Metrics.Queries;

public class GetSubscriptionMetrics(PostgresOptions options) : IPostgresDatabaseQuery<GetSubscriptionMetricsResult>
{
    public async Task<GetSubscriptionMetricsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            WITH lagging AS (
                WITH lagging_subscriptions AS (
                    SELECT COUNT(*) AS lagging
                    FROM {options.Schema}.subscriptions s
                    INNER JOIN {options.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                    WHERE s.status = 'active'
                    AND c.status = 'active'
                    AND c.lagging = TRUE
                    GROUP BY c.group_name, c.name
                )
                SELECT count(*) as lagging FROM lagging_subscriptions
                UNION ALL
                SELECT 0
                LIMIT 1
            ),
            retries AS (
                SELECT count(*) as retries
                FROM {options.Schema}.subscriptions s
                INNER JOIN {options.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status != 'uninitialized'
                AND c.status = 'retry'
             ),
            failed AS (
                SELECT count(*) as failed
                FROM {options.Schema}.subscriptions s
                INNER JOIN {options.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status != 'uninitialized'
                AND c.status = 'failed'
            )
            SELECT l.lagging, r.retries, f.failed
            FROM lagging AS l, retries AS r, failed AS f;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return new GetSubscriptionMetricsResult(
            reader.GetFieldValue<long>(0),
            reader.GetFieldValue<long>(1),
            reader.GetFieldValue<long>(2)
        );
    }
}
