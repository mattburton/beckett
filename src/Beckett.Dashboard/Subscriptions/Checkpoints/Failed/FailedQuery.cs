using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Failed;

public class FailedQuery(
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<FailedQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT c.id, sg.name as group_name, s.name, c.stream_name, c.stream_position, c.updated_at, count(*) over() as total_results
            FROM beckett.checkpoints c
            INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            WHERE c.status = 'failed'
            AND ($1 is null or (sg.name ILIKE '%' || $1 || '%' OR s.name ILIKE '%' || $1 || '%' OR c.stream_name ILIKE '%' || $1 || '%'))
            ORDER BY c.updated_at desc, sg.name, s.name, c.stream_name, c.stream_position
            OFFSET $2
            LIMIT $3;
        """;

        command.CommandText = Query.Build(nameof(FailedQuery), sql, out var prepare);

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

        var results = new List<Result.Failure>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(6);

            results.Add(
                new Result.Failure(
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<string>(3),
                    reader.GetFieldValue<long>(4),
                    reader.GetFieldValue<DateTimeOffset>(5)
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Failure> Failures, int TotalResults)
    {
        public record Failure(
            long Id,
            string GroupName,
            string Name,
            string StreamName,
            long StreamPosition,
            DateTimeOffset LastAttempted
        );
    }
}
