using System.Text.Json;
using Beckett.Database;
using Beckett.Messages;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Message;

public class MessageByStreamPositionQuery(string streamName, long streamPosition)
    : IPostgresDatabaseQuery<MessageResult?>
{
    public async Task<MessageResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id::text,
                   beckett.stream_category(m.stream_name) AS category,
                   m.stream_name,
                   m.global_position,
                   m.stream_position,
                   (
                        SELECT MAX(stream_position) as stream_version
                        FROM beckett.messages
                        WHERE stream_name = m.stream_name
                        AND archived = false
                   ) as stream_version,
                   m.type,
                   m.timestamp,
                   m.data,
                   m.metadata
            FROM beckett.messages AS m
            WHERE m.stream_name = $1
            AND m.stream_position = $2
            AND m.archived = false;
        """;

        command.CommandText = Query.Build(nameof(MessageByStreamPositionQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = streamPosition;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
            reader.GetFieldValue<string>(9),
            MessageSerializer.Options
        ) ?? throw new InvalidOperationException(
            $"Unable to deserialize metadata for message at {streamName} and stream_position {streamPosition}"
        );

        return !reader.HasRows
            ? null
            : new MessageResult(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<long>(5),
                reader.GetFieldValue<string>(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.GetFieldValue<string>(8),
                metadata
            );
    }
}
