using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetSubscriptionStreams(
    string? category,
    string? streamName,
    IReadOnlyList<string> messageTypes,
    long afterStreamIndexId,
    int limit
) : IPostgresDatabaseQuery<IReadOnlyList<GetSubscriptionStreams.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var sql = BuildSql();

        command.CommandText = Query.Build(nameof(GetSubscriptionStreams), sql, out var prepare);

        AddParameters(command);

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        SetParameterValues(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<long>(2),
                    reader.GetFieldValue<long>(3)
                )
            );
        }

        return results;
    }

    private string BuildSql()
    {
        if (!string.IsNullOrWhiteSpace(streamName))
        {
            // Specific stream
            return """
                SELECT si.id, si.stream_name, si.latest_position, si.latest_global_position
                FROM beckett.stream_index si
                WHERE si.stream_name = $1
                AND si.id > $2
                ORDER BY si.id
                LIMIT $3;
            """;
        }

        if (!string.IsNullOrWhiteSpace(category) && messageTypes.Count == 0)
        {
            // Category only
            return """
                SELECT si.id, si.stream_name, si.latest_position, si.latest_global_position
                FROM beckett.stream_index si
                INNER JOIN beckett.stream_categories sc ON si.stream_category_id = sc.id
                WHERE sc.name = $1
                AND si.id > $2
                ORDER BY si.id
                LIMIT $3;
            """;
        }

        if (string.IsNullOrWhiteSpace(category) && messageTypes.Count > 0)
        {
            // Message types only
            return """
                SELECT DISTINCT si.id, si.stream_name, si.latest_position, si.latest_global_position
                FROM beckett.stream_index si
                INNER JOIN beckett.stream_message_types smt ON si.id = smt.stream_index_id
                INNER JOIN beckett.message_types mt ON smt.message_type_id = mt.id
                WHERE mt.name = ANY($1)
                AND si.id > $2
                ORDER BY si.id
                LIMIT $3;
            """;
        }

        if (!string.IsNullOrWhiteSpace(category) && messageTypes.Count > 0)
        {
            // Category and message types
            return """
                SELECT DISTINCT si.id, si.stream_name, si.latest_position, si.latest_global_position
                FROM beckett.stream_index si
                INNER JOIN beckett.stream_categories sc ON si.stream_category_id = sc.id
                INNER JOIN beckett.stream_message_types smt ON si.id = smt.stream_index_id
                INNER JOIN beckett.message_types mt ON smt.message_type_id = mt.id
                WHERE sc.name = $1
                AND mt.name = ANY($2)
                AND si.id > $3
                ORDER BY si.id
                LIMIT $4;
            """;
        }

        // Fallback - shouldn't happen with valid subscriptions
        return """
            SELECT si.id, si.stream_name, si.latest_position, si.latest_global_position
            FROM beckett.stream_index si
            WHERE si.id > $1
            ORDER BY si.id
            LIMIT $2;
        """;
    }

    private void AddParameters(NpgsqlCommand command)
    {
        if (!string.IsNullOrWhiteSpace(streamName))
        {
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        }
        else if (!string.IsNullOrWhiteSpace(category) && messageTypes.Count == 0)
        {
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        }
        else if (string.IsNullOrWhiteSpace(category) && messageTypes.Count > 0)
        {
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        }
        else if (!string.IsNullOrWhiteSpace(category) && messageTypes.Count > 0)
        {
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        }
        else
        {
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        }
    }

    private void SetParameterValues(NpgsqlCommand command)
    {
        var paramIndex = 0;

        if (!string.IsNullOrWhiteSpace(streamName))
        {
            command.Parameters[paramIndex++].Value = streamName;
            command.Parameters[paramIndex++].Value = afterStreamIndexId;
            command.Parameters[paramIndex].Value = limit;
        }
        else if (!string.IsNullOrWhiteSpace(category) && messageTypes.Count == 0)
        {
            command.Parameters[paramIndex++].Value = category;
            command.Parameters[paramIndex++].Value = afterStreamIndexId;
            command.Parameters[paramIndex].Value = limit;
        }
        else if (string.IsNullOrWhiteSpace(category) && messageTypes.Count > 0)
        {
            command.Parameters[paramIndex++].Value = messageTypes.ToArray();
            command.Parameters[paramIndex++].Value = afterStreamIndexId;
            command.Parameters[paramIndex].Value = limit;
        }
        else if (!string.IsNullOrWhiteSpace(category) && messageTypes.Count > 0)
        {
            command.Parameters[paramIndex++].Value = category;
            command.Parameters[paramIndex++].Value = messageTypes.ToArray();
            command.Parameters[paramIndex++].Value = afterStreamIndexId;
            command.Parameters[paramIndex].Value = limit;
        }
        else
        {
            command.Parameters[paramIndex++].Value = afterStreamIndexId;
            command.Parameters[paramIndex].Value = limit;
        }
    }

    public record Result(
        long StreamIndexId,
        string StreamName,
        long LatestPosition,
        long LatestGlobalPosition
    );
}
