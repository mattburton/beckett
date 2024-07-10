using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetSubscriptions : IPostgresDatabaseQuery<GetSubscriptionsResult>
{
    public async Task<GetSubscriptionsResult> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            WITH lagging_checkpoints AS (
                SELECT name, application, SUM(stream_version - stream_position) AS lag
                FROM {schema}.checkpoints
                GROUP BY name, application
            )
            SELECT s.application, s.name, COALESCE(c.lag, 0) AS total_lag
            FROM {schema}.subscriptions s
            LEFT JOIN lagging_checkpoints c ON s.application = c.application AND s.name = c.name
            ORDER BY application, name;
        ";

        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetSubscriptionsResult.Subscription>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetSubscriptionsResult.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<int>(2)
                )
            );
        }

        return new GetSubscriptionsResult(results);
    }
}
