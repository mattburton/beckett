using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryErrorHandler(IRetryManager retryManager)
{
    public Task Handle(RetryError e, CancellationToken cancellationToken) =>
        retryManager.Retry(
            e.Id,
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            e.Attempts,
            cancellationToken
        );
}
