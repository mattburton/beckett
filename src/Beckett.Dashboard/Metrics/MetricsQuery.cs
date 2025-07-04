using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics;

public class MetricsQuery(PostgresOptions options) : IPostgresDatabaseQuery<MetricsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT l.lagging, r.retries, f.failed
            FROM (
                WITH lagging_subscriptions AS (
                    SELECT COUNT(*) AS lagging
                    FROM beckett.subscriptions s
                    INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                    WHERE s.status in ('active', 'replay')
                    AND c.status = 'active'
                    AND c.lagging = TRUE
                    GROUP BY c.group_name, c.name
                )
                SELECT count(1) as lagging FROM lagging_subscriptions
                UNION ALL
                SELECT 0
                LIMIT 1
            ) AS l, (
                SELECT count(1) as retries
                FROM beckett.subscriptions s
                INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status != 'uninitialized'
                AND c.status = 'retry'
            ) AS r, (
                SELECT count(1) as failed
                FROM beckett.subscriptions s
                INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status != 'uninitialized'
                AND c.status = 'failed'
            ) AS f;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return new Result(
            reader.GetFieldValue<long>(0),
            reader.GetFieldValue<long>(1),
            reader.GetFieldValue<long>(2)
        );
    }

    public record Result(long Lagging, long Retries, long Failed);
}
