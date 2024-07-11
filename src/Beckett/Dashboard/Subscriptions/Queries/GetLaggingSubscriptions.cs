using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetLaggingSubscriptions : IPostgresDatabaseQuery<GetLaggingSubscriptionsResult>
{
    public async Task<GetLaggingSubscriptionsResult> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            SELECT application, name, SUM(stream_version - stream_position) AS total_lag
            FROM {schema}.checkpoints
            WHERE (stream_version - stream_position) > 0
            GROUP BY name, application
            ORDER BY SUM(stream_version - stream_position) DESC;
        ";

        await command.PrepareAsync(cancellationToken);

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
