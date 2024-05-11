using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class SubscriptionRetryErrorHandler(IRetryManager retryManager)
{
    public Task Handle(SubscriptionRetryError e, CancellationToken cancellationToken)
    {
        return retryManager.Retry(
            e.SubscriptionName,
            e.Topic,
            e.StreamId,
            e.StreamPosition,
            e.Attempts,
            cancellationToken
        );
    }
}
