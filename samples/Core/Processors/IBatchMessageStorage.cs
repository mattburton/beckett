using Beckett;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Npgsql;
using NpgsqlTypes;

namespace Core.Processors;

public interface IBatchMessageStorage
{
    NpgsqlBatchCommand AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages
    );
}

public class BatchMessageStorage(IInstrumentation instrumentation, PostgresOptions options) : IBatchMessageStorage
{
    private readonly string _sql = $"select {options.Schema}.append_to_stream($1, $2, $3);";

    public NpgsqlBatchCommand AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages
    )
    {
        var activityMetadata = new Dictionary<string, string>();

        using var activity = instrumentation.StartAppendToStreamActivity(streamName, activityMetadata);

        var messagesToAppend = messages.ToList();

        foreach (var message in messagesToAppend)
        {
            message.Metadata.Prepend(activityMetadata);
        }

        var newMessages = messages.Select(x => MessageType.From(streamName, x)).ToArray();

        var command = new NpgsqlBatchCommand(_sql);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageArray(options.Schema) });

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = expectedVersion.Value;
        command.Parameters[2].Value = newMessages;

        return command;
    }
}
