using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryScheduledHandler(IMessageStore messageStore, IRetryProcessor retryProcessor)
{
    public async Task Handle(RetryScheduled message, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(RetryStreamName.For(message.Id), cancellationToken);

        var state = stream.ProjectTo<RetryState>();

        if (state.Completed)
        {
            return;
        }

        await retryProcessor.Retry(state, cancellationToken);
    }
}
