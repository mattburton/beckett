using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Streams;

public class StreamsQuery(
    string tenant,
    string category,
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<StreamsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
           SELECT si.stream_name,
                  max(mi.timestamp) AS last_updated,
                  count(*) over() AS total_results
           FROM beckett.message_index mi
           INNER JOIN beckett.stream_index si ON mi.stream_index_id = si.id
           INNER JOIN beckett.stream_categories sc ON si.stream_category_id = sc.id
           LEFT JOIN beckett.tenants t ON mi.tenant_id = t.id
           WHERE (t.name = $1 OR ($1 = 'default' AND mi.tenant_id IS NULL))
           AND sc.name = $2
           AND ($3 IS NULL OR si.stream_name ILIKE '%' || $3 || '%')
           AND mi.archived = false
           GROUP BY si.stream_name
           ORDER BY max(mi.timestamp) DESC
           OFFSET $4
           LIMIT $5;
        """;

        command.CommandText = Query.Build(nameof(StreamsQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
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
