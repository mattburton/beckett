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
    public async Task Schedule(
        string streamName,
        IEnumerable<object> messages,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    )
    {
        var scheduledMessages = new List<ScheduledMessageType>();

        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartScheduleMessageActivity(streamName, metadata);

        foreach (var message in messages)
        {
            var scheduledMessage = message;
            var messageMetadata = new Dictionary<string, object>(metadata);

            if (message is MessageMetadataWrapper messageWithMetadata)
            {
                foreach (var item in messageWithMetadata.Metadata) messageMetadata.TryAdd(item.Key, item.Value);

                scheduledMessage = messageWithMetadata.Message;
            }

            scheduledMessages.Add(
                ScheduledMessageType.From(
                    scheduledMessage,
                    messageMetadata,
                    deliverAt,
                    messageSerializer
                )
            );
        }

        await database.Execute(new ScheduleMessages(streamName, scheduledMessages.ToArray()), cancellationToken);
    }
}
