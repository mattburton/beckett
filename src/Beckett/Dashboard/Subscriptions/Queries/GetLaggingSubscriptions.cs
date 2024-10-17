using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetLaggingSubscriptions(
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetLaggingSubscriptionsResult>
{
    public async Task<GetLaggingSubscriptionsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT c.group_name,
                   c.name,
                   sum(greatest(0, c.stream_version - c.stream_position)) AS total_lag,
                   count(*) over() as total_results
            FROM {options.Schema}.subscriptions s
            INNER JOIN {options.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
            WHERE s.status = 'active'
            AND c.status = 'active'
            AND c.lagging = true
            GROUP BY c.group_name, c.name
            ORDER BY c.group_name, sum(greatest(0, c.stream_version - c.stream_position)) DESC, name
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
