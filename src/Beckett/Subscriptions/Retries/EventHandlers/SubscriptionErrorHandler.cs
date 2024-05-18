using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class SubscriptionErrorHandler(IRetryManager retryManager)
{
    public Task Handle(SubscriptionError e, CancellationToken cancellationToken) =>
        retryManager.Retry(
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            0,
            cancellationToken
        );
}
