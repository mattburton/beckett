using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class ManualRetryRequestedHandler(IMessageStore messageStore, IRetryProcessor retryProcessor)
{
    public async Task Handle(ManualRetryRequested message, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(RetryStreamName.For(message.Id), cancellationToken);

        var state = stream.ProjectTo<RetryState>();

        await retryProcessor.ManualRetry(state, cancellationToken);
    }
}
