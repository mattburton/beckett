using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Scheduling.Queries;
using Cronos;

namespace Beckett.Scheduling;

public class MessageScheduler(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IInstrumentation instrumentation
) : IMessageScheduler
{
    public Task CancelScheduledMessage(Guid id, CancellationToken cancellationToken)
    {
        return database.Execute(new CancelScheduledMessage(id), cancellationToken);
    }

    public async Task<Guid> ScheduleMessage<TMessage>(
        string streamName,
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) where TMessage : class
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var activityMetadata = new Dictionary<string, string>();

        using var activity = instrumentation.StartScheduleMessageActivity(streamName, activityMetadata);

        var id = MessageId.New();

        if (message is not Message envelope)
        {
            envelope = new Message(message);
        }

        envelope.Metadata.Prepend(activityMetadata);

        var scheduledMessage = ScheduledMessageType.From(
            id,
            envelope,
            DateTimeOffset.UtcNow.Add(delay)
        );

        await database.Execute(
            new ScheduleMessage(streamName, scheduledMessage),
            connection,
            cancellationToken
        );

        return id;
    }

    public async Task ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        string streamName,
        TMessage message,
        CancellationToken cancellationToken
    ) where TMessage : class
    {
        if (!CronExpression.TryParse(cronExpression, out var parsedCronExpression))
        {
            throw new InvalidOperationException("Invalid cron expression");
        }

        var nextOccurrence = parsedCronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);

        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("Unable to calculate next occurrence for cron expression");
        }

        if (message is not Message envelope)
        {
            envelope = new Message(message);
        }

        await database.Execute(
            new AddOrUpdateRecurringMessage(
                name,
                cronExpression,
                streamName,
                envelope,
                nextOccurrence.Value
            ),
            cancellationToken
        );
    }
}
