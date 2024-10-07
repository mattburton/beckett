namespace Beckett;

public interface IMessageScheduler
{
    /// <summary>
    /// Cancel a previously scheduled message using the ID that was returned when the message was originally
    /// scheduled. Typically, this ID is carried forward in messages that are written to the message store so that
    /// a scheduled message can be cancelled if conditions call for it, i.e. we scheduled a message to send a
    /// reminder email to the user to finish setting up their account if they haven't done so after 10 days. If they
    /// set up their account before the time is up we can proactively cancel the scheduled message.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CancelScheduledMessage(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Schedule a message to be sent to a stream in the future at the specified time.
    /// </summary>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="deliverAt">The time to send the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the scheduled message that can be used to cancel it later if needed</returns>
    Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    );
}

public static class MessageSchedulerExtensions
{
    /// <summary>
    /// Schedule a message to be sent to a stream in the future at the specified time.
    /// </summary>
    /// <param name="messageScheduler">THe message scheduler</param>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="deliverAt">The time to send the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the scheduled message that can be used to cancel it later if needed</returns>
    public static Task<Guid> ScheduleMessage(
        this IMessageScheduler messageScheduler,
        string streamName,
        object message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    ) => messageScheduler.ScheduleMessage(streamName, new Message(message), deliverAt, cancellationToken);

    /// <summary>
    /// Schedule a message to be sent to a stream in the future, after the specified delay passes.
    /// </summary>
    /// <param name="messageScheduler">The message scheduler</param>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="delay">The delay before sending the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the scheduled message that can be used to cancel it later if needed</returns>
    public static Task<Guid> ScheduleMessage(
        this IMessageScheduler messageScheduler,
        string streamName,
        object message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => messageScheduler.ScheduleMessage(streamName, new Message(message), DateTimeOffset.UtcNow.Add(delay), cancellationToken);

    /// <summary>
    /// Schedule a message to be sent to a stream in the future, after the specified delay passes.
    /// </summary>
    /// <param name="messageScheduler">The message scheduler</param>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="delay">The delay before sending the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the scheduled message that can be used to cancel it later if needed</returns>
    public static Task<Guid> ScheduleMessage(
        this IMessageScheduler messageScheduler,
        string streamName,
        Message message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => messageScheduler.ScheduleMessage(streamName, message, DateTimeOffset.UtcNow.Add(delay), cancellationToken);
}
