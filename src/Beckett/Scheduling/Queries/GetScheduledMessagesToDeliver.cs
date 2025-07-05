using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class GetScheduledMessagesToDeliver(
    int batchSize
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresScheduledMessage>>
{
    public async Task<IReadOnlyList<PostgresScheduledMessage>> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            WITH messages_to_deliver AS (
                DELETE FROM beckett.scheduled_messages
                WHERE id IN (
                  SELECT id
                  FROM beckett.scheduled_messages
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

        command.CommandText = Query.Build(nameof(GetScheduledMessagesToDeliver), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
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
