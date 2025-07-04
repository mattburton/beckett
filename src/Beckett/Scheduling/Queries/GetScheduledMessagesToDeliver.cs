using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class GetScheduledMessagesToDeliver(
    int batchSize,
    PostgresOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresScheduledMessage>>
{
    public async Task<IReadOnlyList<PostgresScheduledMessage>> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $"""
            WITH messages_to_deliver AS (
                DELETE FROM {options.Schema}.scheduled_messages
                WHERE id IN (
                  SELECT id
                  FROM {options.Schema}.scheduled_messages
                  WHERE deliver_at <= CURRENT_TIMESTAMP
                  FOR UPDATE
                  SKIP LOCKED
                  LIMIT $1
                )
                RETURNING *
            )
            SELECT id,
                   stream_name,
                   type,
                   data,
                   metadata,
                   deliver_at,
                   timestamp
            FROM messages_to_deliver;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = batchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresScheduledMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresScheduledMessage.From(reader));
        }

        return results;
    }
}
