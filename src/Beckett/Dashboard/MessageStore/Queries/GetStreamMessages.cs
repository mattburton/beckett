using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetStreamMessages(
    string streamName,
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetStreamMessagesResult>
{
    public async Task<GetStreamMessagesResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select id, stream_position, type, timestamp, count(*) over() as total_results
            from {options.Schema}.messages
            where stream_name = $1
            and ($2 is null or (id::text ilike '%' || $2 || '%' or type ilike '%' || $2 || '%'))
            and archived = false
            order by stream_position
            offset $3
            limit $4;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[2].Value = offset;
        command.Parameters[3].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetStreamMessagesResult.Message>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(4);

            results.Add(
                new GetStreamMessagesResult.Message(
                    reader.GetFieldValue<Guid>(0),
                    reader.GetFieldValue<int>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<DateTimeOffset>(3)
                )
            );
        }

        return new GetStreamMessagesResult(results, totalResults.GetValueOrDefault(0));
    }
}
