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
                    count(DISTINCT sm.stream_name) as total_streams,
                    count(DISTINCT sm.category) as total_categories,
                    sum(sm.message_count) as total_messages,
                    max(sm.last_updated_at) as last_activity
                FROM beckett.stream_metadata sm
            ),
            type_stats AS (
                SELECT count(DISTINCT st.message_type) as total_message_types
                FROM beckett.stream_types st
            ),
            tenant_stats AS (
                SELECT count(DISTINCT t.tenant) as total_tenants
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