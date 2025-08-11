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
               SELECT si.stream_name,
                      si.last_updated_at,
                      si.message_count,
                      si.latest_position,
                      array_agg(DISTINCT mt.name ORDER BY mt.name) as message_types,
                      array_agg(DISTINCT t.name ORDER BY t.name) FILTER (WHERE t.name IS NOT NULL) as tenants,
                      count(*) over() AS total_results
               FROM beckett.stream_index si
               LEFT JOIN beckett.stream_message_types smt ON si.id = smt.stream_index_id
               LEFT JOIN beckett.message_types mt ON smt.message_type_id = mt.id
               LEFT JOIN beckett.message_index mi ON si.id = mi.stream_index_id AND mi.archived = false
               LEFT JOIN beckett.tenants t ON mi.tenant_id = t.id
               INNER JOIN beckett.stream_categories sc ON si.stream_category_id = sc.id
               WHERE sc.name = $2
               AND ($3 IS NULL OR si.stream_name ILIKE '%' || $3 || '%')
               GROUP BY si.stream_name, si.last_updated_at, si.message_count, si.latest_position
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