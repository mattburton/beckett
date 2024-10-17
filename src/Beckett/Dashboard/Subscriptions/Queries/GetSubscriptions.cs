using Beckett.Database;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetSubscriptions(
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetSubscriptionsResult>
{
    public async Task<GetSubscriptionsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT group_name, name, status, count(*) over() as total_results
            FROM {options.Schema}.subscriptions
            ORDER BY group_name, name
            OFFSET $1
            LIMIT $2;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = offset;
        command.Parameters[1].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetSubscriptionsResult.Subscription>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(3);

            results.Add(
                new GetSubscriptionsResult.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<SubscriptionStatus>(2)
                )
            );
        }

        return new GetSubscriptionsResult(results, totalResults.GetValueOrDefault(0));
    }
}
