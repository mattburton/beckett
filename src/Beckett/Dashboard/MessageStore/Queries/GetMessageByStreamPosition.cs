using System.Text.Json;
using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetMessageByStreamPosition(string streamName, long streamPosition)
    : IPostgresDatabaseQuery<GetMessageResult?>
{
    public async Task<GetMessageResult?> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select id::text, {schema}.stream_category(stream_name) as category, stream_name, type, data, metadata
            from {schema}.messages
            where stream_name = $1
            and stream_position = $2
            order by stream_position;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = streamPosition;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetFieldValue<string>(4)) ??
                       throw new InvalidOperationException(
                           $"Unable to deserialize metadata for message at {streamName} and stream_position {streamPosition}"
                       );

        return !reader.HasRows
            ? null
            : new GetMessageResult(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.GetFieldValue<string>(4),
                metadata
            );
    }
}
