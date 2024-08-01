using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryStartedHandler(IRetryManager retryManager)
{
    public Task Handle(RetryStarted e, CancellationToken cancellationToken) =>
        retryManager.Retry(
            e.CheckpointId,
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            0,
            cancellationToken
        );
}
