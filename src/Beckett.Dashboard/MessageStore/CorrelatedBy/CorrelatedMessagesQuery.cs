using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.CorrelatedBy;

public class CorrelatedMessagesQuery(
    string correlationId,
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<CorrelatedMessagesQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT
                mi.id,
                si.stream_name,
                mi.stream_position,
                mt.name as type,
                mi.timestamp,
                count(*) over() AS total_results
            FROM beckett.message_index mi
            INNER JOIN beckett.stream_index si ON mi.stream_index_id = si.id
            INNER JOIN beckett.message_types mt ON mi.message_type_id = mt.id
            WHERE mi.correlation_id = $1
            AND ($2 IS NULL OR (mi.id::text ILIKE '%' || $2 || '%' OR si.stream_name ILIKE '%' || $2 || '%' OR mt.name ILIKE '%' || $2 || '%'))
            AND mi.archived = false
            ORDER BY mi.global_position
            OFFSET $3
            LIMIT $4;
        """;

        command.CommandText = Query.Build(nameof(CorrelatedMessagesQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = correlationId;
        command.Parameters[1].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[2].Value = offset;
        command.Parameters[3].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result.Message>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(5);

            results.Add(
                new Result.Message(
                    reader.GetFieldValue<Guid>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<int>(2),
                    reader.GetFieldValue<string>(3),
                    reader.GetFieldValue<DateTimeOffset>(4)
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Message> Messages, int TotalResults)
    {
        public record Message(Guid Id, string StreamName, int StreamPosition, string Type, DateTimeOffset Timestamp);
    }
}
