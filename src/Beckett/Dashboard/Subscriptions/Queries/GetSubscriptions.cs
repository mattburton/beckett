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
            SELECT s.application, s.name
            FROM {schema}.subscriptions s
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
                    reader.GetFieldValue<string>(1)
                )
            );
        }

        return new GetSubscriptionsResult(results);
    }
}
