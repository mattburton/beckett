using System.Text.Json;
using Beckett.Database;
using Beckett.Messages;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Scheduled.Message;

public class MessageQuery(Guid id, PostgresOptions options) : IPostgresDatabaseQuery<Message.ViewModel?>
{
    public async Task<Message.ViewModel?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT stream_name,
                   type,
                   deliver_at,
                   timestamp,
                   data,
                   metadata
            FROM {options.Schema}.scheduled_messages
            WHERE id = $1;
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
            reader.GetFieldValue<string>(5),
            MessageSerializer.Options
        ) ?? throw new InvalidOperationException($"Unable to deserialize metadata for scheduled message {id}");

        return !reader.HasRows
            ? null
            : new Message.ViewModel(
                id,
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<string>(4),
                metadata
            );
    }
}
