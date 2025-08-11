using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.MessageStore.Components.Queries;

public class StreamStatsQuery : IPostgresDatabaseQuery<StreamStatsQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH stats AS (
                SELECT 
                    count(DISTINCT si.stream_name) as total_streams,
                    count(DISTINCT sc.name) as total_categories,
                    sum(si.message_count) as total_messages,
                    max(si.last_updated_at) as last_activity
                FROM beckett.stream_index si
                INNER JOIN beckett.stream_categories sc ON si.stream_category_id = sc.id
            ),
            type_stats AS (
                SELECT count(DISTINCT mt.name) as total_message_types
                FROM beckett.stream_message_types smt
                INNER JOIN beckett.message_types mt ON smt.message_type_id = mt.id
            ),
            tenant_stats AS (
                SELECT count(DISTINCT t.name) as total_tenants
                FROM beckett.tenants t
            )
            SELECT 
                s.total_streams,
                s.total_categories,
                s.total_messages,
                s.last_activity,
                ts.total_message_types,
                tn.total_tenants
            FROM stats s
            CROSS JOIN type_stats ts  
            CROSS JOIN tenant_stats tn;
        """;

        command.CommandText = Query.Build(nameof(StreamStatsQuery), sql, out var prepare);

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new Result(
                reader.GetFieldValue<long>(0),
                reader.GetFieldValue<long>(1), 
                reader.GetFieldValue<long>(2),
                reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<long>(5)
            );
        }

        return new Result(0, 0, 0, null, 0, 0);
    }

    public record Result(
        long TotalStreams,
        long TotalCategories,
        long TotalMessages,
        DateTimeOffset? LastActivity,
        long TotalMessageTypes,
        long TotalTenants
    );
}