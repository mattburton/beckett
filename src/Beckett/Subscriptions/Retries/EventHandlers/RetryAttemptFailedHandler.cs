using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryAttemptFailedHandler(
    IMessageStore messageStore,
    IMessageScheduler messageScheduler,
    ILogger<RetryAttemptFailedHandler> logger
)
{
    public async Task Handle(RetryAttemptFailed message, CancellationToken cancellationToken)
    {
        var streamName = RetryStreamName.For(message.Id);

        var stream = await messageStore.ReadStream(streamName, cancellationToken);

        var state = stream.ProjectTo<RetryState>();

        if (state.Completed)
        {
            return;
        }

        logger.LogTrace(
            "Retry attempt failed for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - scheduling retry at {RetryAt}",
            state.GroupName,
            state.Name,
            state.StreamName,
            state.StreamPosition,
            state.Attempts,
            state.MaxRetryCount,
            message.RetryAt
        );

        var nextAttempt = state.Attempts + 1;

        await messageScheduler.ScheduleMessage(
            streamName,
            new RetryScheduled(
                message.Id,
                nextAttempt,
                message.RetryAt,
                DateTimeOffset.UtcNow
            ),
            message.RetryAt,
            cancellationToken
        );
    }
}
