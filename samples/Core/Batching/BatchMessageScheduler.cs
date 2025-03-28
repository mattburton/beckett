using Beckett;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Npgsql;
using NpgsqlTypes;

namespace Core.Batching;

public class BatchMessageScheduler(PostgresOptions options) : IBatchMessageScheduler
{
    private readonly string _sql = $"select {options.Schema}.schedule_message($1, $2);";

    public NpgsqlBatchCommand ScheduleMessage(string streamName, Message message, DateTimeOffset deliverAt)
    {
        var id = MessageId.New();

        var scheduledMessage = ScheduledMessageType.From(
            id,
            message,
            deliverAt
        );

        var command = new NpgsqlBatchCommand(_sql);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.ScheduledMessage(options.Schema) });

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = scheduledMessage;

        return command;
    }
}
