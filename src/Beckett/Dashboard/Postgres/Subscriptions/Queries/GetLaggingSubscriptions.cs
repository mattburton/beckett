using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.Subscriptions.Queries;

public class GetLaggingSubscriptions(
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetLaggingSubscriptionsResult>
{
    public async Task<GetLaggingSubscriptionsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT g.name,
                   s.name,
                   sum(greatest(0, c.stream_version - c.stream_position)) AS total_lag,
                   count(*) over() as total_results
            FROM {options.Schema}.subscriptions s
            INNER JOIN {options.Schema}.groups g ON s.group_id = g.id
            INNER JOIN {options.Schema}.checkpoints c ON s.id = c.subscription_id
            WHERE s.status = 'active'
            AND c.status = 'active'
            AND c.lagging = true
            GROUP BY g.name, s.name
            ORDER BY g.name, sum(greatest(0, c.stream_version - c.stream_position)) DESC, s.name
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

        var results = new List<GetLaggingSubscriptionsResult.Subscription>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(3);

            results.Add(
                new GetLaggingSubscriptionsResult.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<int>(2)
                )
            );
        }

        return new GetLaggingSubscriptionsResult(results, totalResults.GetValueOrDefault(0));
    }
}
