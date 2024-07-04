using System.Text.Json;
using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetMessage(Guid id) : IPostgresDatabaseQuery<GetMessageResult?>
{
    public async Task<GetMessageResult?> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select {schema}.stream_category(stream_name) as category, stream_name, type, data, metadata
            from {schema}.messages
            where id = $1
            order by stream_position;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetFieldValue<string>(4)) ??
                       throw new InvalidOperationException($"Unable to deserialize metadata for message {id}");

        return !reader.HasRows
            ? null
            : new GetMessageResult(
                id.ToString(),
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                metadata
            );
    }
}
