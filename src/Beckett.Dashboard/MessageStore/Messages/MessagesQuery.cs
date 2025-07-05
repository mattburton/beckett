using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Messages;

public class MessagesQuery(
    string streamName,
    string? query,
    int offset,
    int limit
) : IPostgresDatabaseQuery<MessagesQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, stream_position, type, timestamp, count(*) over() AS total_results
            FROM beckett.messages
            WHERE stream_name = $1
            AND ($2 IS NULL OR (id::text ILIKE '%' || $2 || '%' OR type ILIKE '%' || $2 || '%'))
            AND archived = false
            ORDER BY stream_position
            OFFSET $3
            LIMIT $4;
        """;

        command.CommandText = Query.Build(nameof(MessagesQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[2].Value = offset;
        command.Parameters[3].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result.Message>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(4);

            results.Add(
                new Result.Message(
                    reader.GetFieldValue<Guid>(0),
                    reader.GetFieldValue<int>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<DateTimeOffset>(3)
                )
            );
        }

        return new Result(results, totalResults.GetValueOrDefault(0));
    }

    public record Result(List<Result.Message> Messages, int TotalResults)
    {
        public record Message(Guid Id, int StreamPosition, string Type, DateTimeOffset Timestamp);
    }
}
