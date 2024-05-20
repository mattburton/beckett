using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Cronos;

namespace Beckett.Scheduling;

public class MessageScheduler(
    BeckettOptions options,
    IPostgresDatabase database,
    IMessageSerializer messageSerializer,
    IInstrumentation instrumentation
) : IMessageScheduler
{
    public async Task Once(string name, string streamName, object message, CancellationToken cancellationToken)
    {
        var (type, data, metadata) = BuildRecurringMessage(message);

        await database.Execute(
            new AddOrUpdateRecurringMessage(
                options.ApplicationName,
                name,
                null,
                streamName,
                type,
                data,
                metadata,
                null
            ),
            cancellationToken
        );
    }

    public async Task Recurring(
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

        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Scheduling.TimeZone);
        }
        catch (TimeZoneNotFoundException e)
        {
            throw new InvalidOperationException(
                $"Invalid time zone {options.Scheduling.TimeZone} specified in scheduling options",
                e
            );
        }

        var nextOccurrence = parsedCronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, timeZone);

        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("Unable to calculate next occurrence for cron expression");
        }

        await database.Execute(
            new AddOrUpdateRecurringMessage(
                options.ApplicationName,
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
