using Beckett.Storage.Postgres.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class ScheduleEventsQuery
{
    public static async Task Execute(
        NpgsqlConnection connection,
        string schema,
        string streamName,
        NewScheduledEvent[] scheduledEvents,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select {schema}.schedule_events($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = NewScheduledEvent.DataTypeNameFor(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = scheduledEvents;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
