using System.Text.Json;
using Beckett.Database;
using Beckett.Messages;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.MessageStore.Queries;

public class GetMessageByStreamPosition(string streamName, long streamPosition, PostgresOptions options)
    : IPostgresDatabaseQuery<GetMessageResult?>
{
    public async Task<GetMessageResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT id::text,
                   {options.Schema}.stream_category(m.stream_name) AS category,
                   m.stream_name,
                   m.global_position,
                   m.stream_position,
                   (
                        SELECT MAX(stream_position) as stream_version
                        FROM {options.Schema}.messages
                        WHERE stream_name = m.stream_name
                        AND archived = false
                   ) as stream_version,
                   m.type,
                   m.timestamp,
                   m.data,
                   m.metadata
            FROM {options.Schema}.messages AS m
            WHERE m.stream_name = $1
            AND m.stream_position = $2
            AND m.archived = false;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (options.PrepareStatements)
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
            : new GetMessageResult(
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
