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
    /// <para>
    /// Add a recurring message. This is useful for scheduled tasks or other activities that occur on a regular
    /// interval. A recurring message has a name that is unique to the current subscription group, and a cron expression
    /// that configures the schedule. When the next occurrence comes due the supplied message will be appended to the
    /// specified stream. By subscribing to the message type you can then support scheduled tasks / jobs within your
    /// application using the same stream-based processing you are using elsewhere, including retries.
    /// </para>
    /// <para>
    /// Note that recurring messages will be written to the message store based on the interval specified by the cron
    /// expression you supply. If your application has not handled the previous message before the next occurrence comes
    /// due then they might overlap. If this is a concern we recommend using the <see cref="IMessageScheduler"/>
    /// instead to implement scheduled tasks where a subscription handler is responsible for scheduling the next
    /// occurrence manually once the current one succeeds.
    /// </para>
    /// <para>
    /// The cron expression will be parsed and validated to ensure it is a valid expression. Please see the Cronos
    /// <see href="https://github.com/HangfireIO/Cronos#cron-format">documentation</see> for specific cron expression
    /// feature support.
    /// </para>
    /// </summary>
    /// <param name="name">The name of the recurring message</param>
    /// <param name="cronExpression">The cron expression for the recurring message</param>
    /// <param name="streamName">The stream to append the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken"></param>
    Task RecurringMessage(
        string name,
        string cronExpression,
        string streamName,
        Message message,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Schedule a message to be sent to a stream in the future, after the specified delay passes.
    /// </summary>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="delay">The delay before sending the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the scheduled message that can be used to call <see cref="CancelScheduledMessage"/></returns>
    Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => ScheduleMessage(streamName, message, DateTimeOffset.UtcNow.Add(delay), cancellationToken);

    /// <summary>
    /// Schedule a message to be sent to a stream in the future at the specified time.
    /// </summary>
    /// <param name="streamName">The stream name to send the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="deliverAt">The time to send the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the scheduled message that can be used to call <see cref="CancelScheduledMessage"/></returns>
    Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    );
}
