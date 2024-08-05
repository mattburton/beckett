using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Scheduling.Queries;
using Cronos;
using UUIDNext;

namespace Beckett.Scheduling;

public class MessageScheduler(
    IPostgresDatabase database,
    IMessageSerializer messageSerializer,
    IInstrumentation instrumentation
) : IMessageScheduler
{
    public Task CancelScheduledMessage(Guid id, CancellationToken cancellationToken)
    {
        return database.Execute(new CancelScheduledMessage(id), cancellationToken);
    }

    public async Task RecurringMessage(
        string name,
        string cronExpression,
        string streamName,
        object message,
        CancellationToken cancellationToken
    )
    {
        var (type, data, metadata) = BuildRecurringMessage(message);

        if (!CronExpression.TryParse(cronExpression, out var parsedCronExpression))
        {
            throw new InvalidOperationException("Invalid cron expression");
        }

        var nextOccurrence = parsedCronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);

        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("Unable to calculate next occurrence for cron expression");
        }

        await database.Execute(
            new AddOrUpdateRecurringMessage(
                name,
                cronExpression,
                streamName,
                type,
                data,
                metadata,
                nextOccurrence.Value
            ),
            cancellationToken
        );
    }

    public async Task<Guid> ScheduleMessage(
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

        var id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);

        var scheduledMessage = ScheduledMessageType.From(
            id,
            messageToSchedule,
            messageMetadata,
            deliverAt,
            messageSerializer
        );

        await database.Execute(
            new ScheduleMessage(streamName, scheduledMessage),
            cancellationToken
        );

        return id;
    }

    private (string Type, string Data, string Metadata) BuildRecurringMessage(object message)
    {
        var recurringMessage = message;
        var metadata = new Dictionary<string, object>();

        if (message is MessageMetadataWrapper messageWithMetadata)
        {
            foreach (var item in messageWithMetadata.Metadata)
            {
                metadata.TryAdd(item.Key, item.Value);
            }

            recurringMessage = messageWithMetadata.Message;
        }

        var (_, type, data, metadataJson) = messageSerializer.Serialize(recurringMessage, metadata);

        return (type, data, metadataJson);
    }
}
