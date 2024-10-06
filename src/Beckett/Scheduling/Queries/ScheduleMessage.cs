using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class ScheduleMessage(
    string streamName,
    ScheduledMessageType scheduledMessage,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.schedule_message($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.ScheduledMessage(options.Schema) });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = scheduledMessage;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
