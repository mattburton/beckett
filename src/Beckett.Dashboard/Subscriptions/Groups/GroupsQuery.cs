using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Groups;

public class GroupsQuery(
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<GroupsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT group_name, count(*) over() as total_results
            FROM beckett.subscriptions
            WHERE ($1 IS NULL or name ILIKE '%' || $1 || '%')
            GROUP BY group_name
            ORDER BY group_name
            OFFSET $2
            LIMIT $3;
        """;

        command.CommandText = Query.Build(nameof(GroupsQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[1].Value = offset;
        command.Parameters[2].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result.Group>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(1);

            results.Add(new Result.Group(reader.GetFieldValue<string>(0)));
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Group> Groups, int TotalResults)
    {
        public record Group(string Name);
    }
}
