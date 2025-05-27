using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Streams;

public class StreamsQuery(
    string tenant,
    string category,
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<StreamsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
           SELECT stream_name,
                  max(timestamp) AS last_updated,
                  count(*) over() AS total_results
           FROM {options.Schema}.messages_active
           WHERE metadata ->> '$tenant' = $1
           AND {options.Schema}.stream_category(stream_name) = $2
           AND ($3 IS NULL OR stream_name ILIKE '%' || $3 || '%')
           GROUP BY stream_name
           ORDER BY max(timestamp) DESC
           OFFSET $4
           LIMIT $5;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = tenant;
        command.Parameters[1].Value = category;
        command.Parameters[2].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[3].Value = offset;
        command.Parameters[4].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result.Stream>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(2);

            results.Add(
                new Result.Stream(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<DateTimeOffset>(1)
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Stream> Streams, int TotalResults)
    {
        public record Stream(string StreamName, DateTimeOffset LastUpdated);
    }
}
