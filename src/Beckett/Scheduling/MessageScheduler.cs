using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Scheduling.Queries;

namespace Beckett.Scheduling;

public class MessageScheduler(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IInstrumentation instrumentation,
    PostgresOptions options
) : IMessageScheduler
{
    public Task CancelScheduledMessage(Guid id, CancellationToken cancellationToken)
    {
        return database.Execute(new CancelScheduledMessage(id, options), cancellationToken);
    }

    public async Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    )
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var activityMetadata = new Dictionary<string, string>();

        using var activity = instrumentation.StartScheduleMessageActivity(streamName, activityMetadata);

        var id = MessageId.New();

        message.Metadata.Prepend(activityMetadata);

        var scheduledMessage = ScheduledMessageType.From(
            id,
            message,
            deliverAt
        );

        await database.Execute(
            new ScheduleMessage(streamName, scheduledMessage, options),
            connection,
            cancellationToken
        );

        return id;
    }
}
