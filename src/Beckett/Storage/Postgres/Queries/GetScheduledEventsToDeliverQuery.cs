using Beckett.Storage.Postgres.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class GetScheduledEventsToDeliverQuery
{
    public static async Task<IReadOnlyList<ScheduledEvent>> Execute(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;

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

        var results = new List<ScheduledEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ScheduledEvent.From(reader));
        }

        return results;
    }
}
