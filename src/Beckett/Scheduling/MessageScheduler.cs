using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Scheduling.Queries;
using Cronos;
using Npgsql;
using UUIDNext;

namespace Beckett.Scheduling;

public class MessageScheduler(
    IPostgresDatabase database,
    IInstrumentation instrumentation,
    PostgresOptions options
) : IMessageScheduler, ITransactionalMessageScheduler
{
    public Task CancelScheduledMessage(Guid id, CancellationToken cancellationToken)
    {
        return database.Execute(new CancelScheduledMessage(id, options), cancellationToken);
    }

    public async Task RecurringMessage(
        string name,
        string cronExpression,
        string streamName,
        Message message,
        CancellationToken cancellationToken
    )
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

        await database.Execute(
            new AddOrUpdateRecurringMessage(
                name,
                cronExpression,
                streamName,
                message,
                nextOccurrence.Value,
                options
            ),
            cancellationToken
        );
    }

    public async Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var id = await ScheduleMessage(streamName, message, deliverAt, connection, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return id;
    }

    public async Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        var activityMetadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartScheduleMessageActivity(streamName, activityMetadata);

        var id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);

        message.Metadata.Prepend(activityMetadata);

        var scheduledMessage = ScheduledMessageType.From(
            id,
            message,
            deliverAt
        );

        await database.Execute(
            new ScheduleMessage(streamName, scheduledMessage, options),
            connection,
            transaction,
            cancellationToken
        );

        return id;
    }
}
