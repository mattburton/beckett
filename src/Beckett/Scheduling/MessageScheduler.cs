using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;

namespace Beckett.Scheduling;

public class MessageScheduler(
    IPostgresDatabase database,
    IMessageSerializer messageSerializer,
    IInstrumentation instrumentation
) : IMessageScheduler
{
    public Task Cancel(Guid id, CancellationToken cancellationToken)
    {
        return database.Execute(new CancelScheduledMessage(id), cancellationToken);
    }

    public async Task<Guid> Schedule(
        string streamName,
        object message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    )
    {
        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartScheduleMessageActivity(streamName, metadata);

        var messageToSchedule = message;
        var messageMetadata = new Dictionary<string, object>(metadata);

        if (message is MessageMetadataWrapper messageWithMetadata)
        {
            foreach (var item in messageWithMetadata.Metadata) messageMetadata.TryAdd(item.Key, item.Value);

            messageToSchedule = messageWithMetadata.Message;
        }

        var id = Guid.NewGuid();

        var scheduledMessage = ScheduledMessageType.From(
            id,
            messageToSchedule,
            messageMetadata,
            deliverAt,
            messageSerializer
        );

        await database.Execute(new ScheduleMessage(streamName, scheduledMessage), cancellationToken);

        return id;
    }
}
