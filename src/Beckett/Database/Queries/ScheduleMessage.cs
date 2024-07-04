using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ScheduleMessage(
    string application,
    string streamName,
    ScheduledMessageType scheduledMessage
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.schedule_message($1, $2, $3);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.ScheduledMessage(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = streamName;
        command.Parameters[2].Value = scheduledMessage;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
