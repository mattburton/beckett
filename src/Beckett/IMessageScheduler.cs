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
    /// Schedule a message to be sent to a stream in the future, after the specified delay passes.
    /// </summary>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="delay">The delay before sending the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TMessage">The type of message to schedule</typeparam>
    /// <returns>The ID of the scheduled message that can be used to cancel it later if needed</returns>
    Task<Guid> ScheduleMessage<TMessage>(
        string streamName,
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) where TMessage : class;

    /// <summary>
    /// Schedule a recurring message
    /// </summary>
    /// <param name="name">The name of the recurring message</param>
    /// <param name="cronExpression">The cron expression for the recurring message</param>
    /// <param name="timeZone">The time zone used to evaluate the cron schedule</param>
    /// <param name="streamName">The stream to append the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">TCancellation token</param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    Task ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        TimeZoneInfo timeZone,
        string streamName,
        TMessage message,
        CancellationToken cancellationToken
    ) where TMessage : class;
}
