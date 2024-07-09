using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryAttemptedHandler(IRetryManager retryManager)
{
    public Task Handle(RetryAttempted e, CancellationToken cancellationToken) =>
        retryManager.Retry(
            e.Id,
            e.ApplicationName,
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            e.Attempts,
            cancellationToken
        );
}
