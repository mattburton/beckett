using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Lagging;

public class LaggingQuery(
    int offset,
    int limit
) : IPostgresDatabaseQuery<LaggingQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT sg.name as group_name,
                   s.name,
                   sum(greatest(0, cr.target_stream_version - c.stream_position)) AS total_lag,
                   count(*) over() as total_results
            FROM beckett.subscriptions s
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
            INNER JOIN beckett.checkpoints_ready cr ON c.id = cr.id
            WHERE s.status in ('active', 'replay')
            AND c.status = 'active'
            GROUP BY sg.name, s.name
            ORDER BY sg.name, sum(greatest(0, cr.target_stream_version - c.stream_position)) DESC, s.name
            OFFSET $1
            LIMIT $2;
        """;

        command.CommandText = Query.Build(nameof(LaggingQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = offset;
        command.Parameters[1].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result.Subscription>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(3);

            results.Add(
                new Result.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<int>(2)
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(
        List<Result.Subscription> Subscriptions,
        int TotalResults
    )
    {
        public record Subscription(string GroupName, string Name, int TotalLag);
    }
}
