using System.Text.Json;
using Beckett.Database;
using Beckett.Messages;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetMessage(Guid id, PostgresOptions options) : IPostgresDatabaseQuery<GetMessageResult?>
{
    public async Task<GetMessageResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT {options.Schema}.stream_category(m.stream_name) AS category,
                   m.stream_name,
                   m.global_position,
                   m.stream_position,
                   (
                        SELECT MAX(stream_position) as stream_version
                        FROM {options.Schema}.messages
                        WHERE stream_name = m.stream_name
                        AND deleted = false
                   ) as stream_version,
                   m.type,
                   m.timestamp,
                   m.data,
                   m.metadata
            FROM {options.Schema}.messages AS m
            WHERE m.id = $1
            AND m.deleted = false;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
            reader.GetFieldValue<string>(8),
            MessageSerializer.Options
        ) ?? throw new InvalidOperationException($"Unable to deserialize metadata for message {id}");

        return !reader.HasRows
            ? null
            : new GetMessageResult(
                id.ToString(),
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<long>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<string>(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<string>(7),
                metadata
            );
    }
}
