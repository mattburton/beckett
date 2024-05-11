using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class SubscriptionErrorHandler(IRetryManager retryManager)
{
    public Task Handle(SubscriptionError e, CancellationToken cancellationToken)
    {
        return retryManager.Retry(
            e.SubscriptionName,
            e.Topic,
            e.StreamId,
            e.StreamPosition,
            0,
            cancellationToken
        );
    }
}
