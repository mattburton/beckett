namespace Beckett;

public interface IMessageScheduler
{
    Task CancelScheduledMessage(Guid id, CancellationToken cancellationToken);

    Task RecurringMessage(
        string name,
        string cronExpression,
        string streamName,
        object message,
        CancellationToken cancellationToken
    );

    Task<Guid> ScheduleMessage(
        string streamName,
        object message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => ScheduleMessage(streamName, message, DateTimeOffset.UtcNow.Add(delay), cancellationToken);


    Task<Guid> ScheduleMessage(
        string streamName,
        object message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    );
}
