using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ScheduleEvents(string streamName, ScheduledEventType[] scheduledEvents) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.schedule_events($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = ScheduledEventType.DataTypeNameFor(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = scheduledEvents;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
