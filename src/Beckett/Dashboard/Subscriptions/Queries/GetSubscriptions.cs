using Beckett.Database;
using Beckett.Subscriptions;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetSubscriptions(PostgresOptions options) : IPostgresDatabaseQuery<GetSubscriptionsResult>
{
    public async Task<GetSubscriptionsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT group_name, name, status
            FROM {options.Schema}.subscriptions
            ORDER BY group_name, name;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetSubscriptionsResult.Subscription>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetSubscriptionsResult.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<SubscriptionStatus>(2)
                )
            );
        }

        return new GetSubscriptionsResult(results);
    }
}
