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
        const string sql = """
            SELECT c.group_name,
                   c.name,
                   sum(greatest(0, c.stream_version - c.stream_position)) AS total_lag,
                   count(*) over() as total_results
            FROM beckett.subscriptions s
            INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
            WHERE s.status in ('active', 'replay')
            AND c.status = 'active'
            AND c.lagging = true
            GROUP BY c.group_name, c.name
            ORDER BY c.group_name, sum(greatest(0, c.stream_version - c.stream_position)) DESC, name
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
