using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ScheduleMessages(
    string topic,
    string streamId,
    ScheduledMessageType[] scheduledMessages
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.schedule_messages($1, $2, $3);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.ScheduledMessageArray(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = topic;
        command.Parameters[1].Value = streamId;
        command.Parameters[2].Value = scheduledMessages;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
