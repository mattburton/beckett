using Beckett.Database;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Subscriptions;

public class SubscriptionsQuery(
    string groupName,
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<SubscriptionsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, status, count(*) over() as total_results
            FROM beckett.subscriptions
            WHERE group_name = $1
            AND ($2 IS NULL or name ILIKE '%' || $2 || '%')
            ORDER BY name
            OFFSET $3
            LIMIT $4;
        """;

        command.CommandText = Query.Build(nameof(SubscriptionsQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[2].Value = offset;
        command.Parameters[3].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result.Subscription>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(2);

            results.Add(
                new Result.Subscription(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<SubscriptionStatus>(1)
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Subscription> Subscriptions, int TotalResults)
    {
        public record Subscription(string Name, SubscriptionStatus Status);
    }
}
