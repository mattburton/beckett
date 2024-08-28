using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryStartedHandler(
    IMessageStore messageStore,
    IMessageScheduler messageScheduler,
    ILogger<RetryStartedHandler> logger
)
{
    public async Task Handle(RetryStarted message, CancellationToken cancellationToken)
    {
        var streamName = RetryStreamName.For(message.Id);

        var stream = await messageStore.ReadStream(streamName, cancellationToken);

        var state = stream.ProjectTo<RetryState>();

        if (state.Completed)
        {
            return;
        }

        await messageScheduler.ScheduleMessage(
            streamName,
            new RetryScheduled(
                message.Id,
                1,
                message.RetryAt.GetValueOrDefault(),
                DateTimeOffset.UtcNow
            ),
            message.RetryAt.GetValueOrDefault(),
            cancellationToken
        );

        logger.LogTrace(
            "Retry started for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - scheduled first retry at {RetryAt}",
            state.GroupName,
            state.Name,
            state.StreamName,
            state.StreamPosition,
            message.RetryAt
        );
    }
}
