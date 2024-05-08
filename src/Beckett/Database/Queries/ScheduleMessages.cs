using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ScheduleMessages(string streamName, ScheduledMessageType[] scheduledMessages) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.schedule_messages($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = ScheduledMessageType.DataTypeNameFor(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = scheduledMessages;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
