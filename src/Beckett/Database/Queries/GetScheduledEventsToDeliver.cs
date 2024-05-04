using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class GetScheduledEventsToDeliver(int batchSize) : IPostgresDatabaseQuery<IReadOnlyList<PostgresScheduledEvent>>
{
    public async Task<IReadOnlyList<PostgresScheduledEvent>> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select id,
                   stream_name,
                   type,
                   data,
                   metadata,
                   deliver_at,
                   timestamp
            from {schema}.get_scheduled_events_to_deliver($1);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = batchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresScheduledEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresScheduledEvent.From(reader));
        }

        return results;
    }
}
