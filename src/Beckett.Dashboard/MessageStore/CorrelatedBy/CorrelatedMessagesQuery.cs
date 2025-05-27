using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.CorrelatedBy;

public class CorrelatedMessagesQuery(
    string correlationId,
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<CorrelatedMessagesQuery.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
           select id, stream_name, stream_position, type, timestamp, count(*) over() as total_results
           from {options.Schema}.messages
           where metadata->>'$correlation_id' = $1
           and ($2 is null or (id::text ilike '%' || $2 || '%' or stream_name ilike '%' || $2 || '%' or type ilike '%' || $2 || '%'))
           and archived = false
           order by global_position
           offset $3
           limit $4;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
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
