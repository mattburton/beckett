using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Streams;

public class EnhancedStreamsQuery(
    string tenant,
    string category,
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<EnhancedStreamsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
           WITH stream_data AS (
               SELECT sm.stream_name,
                      sm.last_updated_at,
                      sm.message_count,
                      sm.latest_position,
                      array_agg(DISTINCT st.message_type ORDER BY st.message_type) as message_types,
                      array_agg(DISTINCT mm.tenant ORDER BY mm.tenant) FILTER (WHERE mm.tenant IS NOT NULL) as tenants,
                      count(*) over() AS total_results
               FROM beckett.stream_metadata sm
               LEFT JOIN beckett.stream_types st ON sm.stream_name = st.stream_name
               LEFT JOIN beckett.message_metadata mm ON sm.stream_name = mm.stream_name
               WHERE sm.category = $2
               AND ($3 IS NULL OR sm.stream_name ILIKE '%' || $3 || '%')
               GROUP BY sm.stream_name, sm.last_updated_at, sm.message_count, sm.latest_position
           )
           SELECT stream_name,
                  last_updated_at,
                  message_count,
                  latest_position,
                  message_types,
                  tenants,
                  total_results
           FROM stream_data
           WHERE ($1 = 'default' OR $1 = ANY(tenants))
           ORDER BY last_updated_at DESC
           OFFSET $4
           LIMIT $5;
        """;

        command.CommandText = Query.Build(nameof(EnhancedStreamsQuery), sql, out var prepare);

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
            totalResults ??= reader.GetFieldValue<int>(6);

            var messageTypes = reader.IsDBNull(4) ? new string[0] : reader.GetFieldValue<string[]>(4);
            var tenants = reader.IsDBNull(5) ? new string[0] : reader.GetFieldValue<string[]>(5);

            results.Add(
                new Result.Stream(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<DateTimeOffset>(1),
                    reader.GetFieldValue<long>(2),
                    reader.GetFieldValue<long>(3),
                    messageTypes,
                    tenants
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Stream> Streams, int TotalResults)
    {
        public record Stream(
            string StreamName, 
            DateTimeOffset LastUpdated,
            long MessageCount,
            long LatestPosition,
            string[] MessageTypes,
            string[] Tenants
        );
    }
}