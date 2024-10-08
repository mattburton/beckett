using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetLaggingSubscriptions(PostgresOptions options) : IPostgresDatabaseQuery<GetLaggingSubscriptionsResult>
{
    public async Task<GetLaggingSubscriptionsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT c.group_name, c.name, SUM(GREATEST(0, c.stream_version - c.stream_position)) AS total_lag
            FROM {options.Schema}.subscriptions s
            INNER JOIN {options.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
            WHERE s.status = 'active'
            AND c.status = 'active'
            AND c.lagging = true
            GROUP BY c.group_name, c.name
            ORDER BY c.group_name, SUM(GREATEST(0, c.stream_version - c.stream_position)) DESC, name;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetLaggingSubscriptionsResult.Subscription>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetLaggingSubscriptionsResult.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<int>(2)
                )
            );
        }

        return new GetLaggingSubscriptionsResult(results);
    }
}
