using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Failed;

public class FailedQuery(
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<FailedQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT id, group_name, name, stream_name, stream_position, updated_at, count(*) over() as total_results
            FROM {options.Schema}.checkpoints
            WHERE status = 'failed'
            AND ($1 is null or (group_name ILIKE '%' || $1 || '%' OR name ILIKE '%' || $1 || '%' OR stream_name ILIKE '%' || $1 || '%'))
            ORDER BY updated_at desc, group_name, name, stream_name, stream_position
            OFFSET $2
            LIMIT $3;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
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
