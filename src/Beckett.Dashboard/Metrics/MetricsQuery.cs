using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics;

public class MetricsQuery : IPostgresDatabaseQuery<MetricsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT l.lagging, r.retries, f.failed
            FROM (
                SELECT count(s.*) as lagging
                FROM beckett.checkpoints_ready r
                INNER JOIN beckett.checkpoints c ON r.id = c.id
                inner join beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
                WHERE s.status IN ('active', 'replay')
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
        """;

        command.CommandText = Query.Build(nameof(MetricsQuery), sql, out var prepare);

        if (prepare)
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
