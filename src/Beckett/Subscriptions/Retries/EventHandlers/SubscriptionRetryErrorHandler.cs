using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class SubscriptionRetryErrorHandler(IRetryManager retryManager)
{
    public Task Handle(SubscriptionRetryError e, CancellationToken cancellationToken)
    {
        return retryManager.Retry(
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            e.Attempts,
            cancellationToken
        );
    }
}
