using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryAttemptedHandler(IRetryManager retryManager)
{
    public Task Handle(RetryAttempted e, CancellationToken cancellationToken) =>
        retryManager.Retry(
            e.CheckpointId,
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            e.Attempts,
            cancellationToken
        );
}
