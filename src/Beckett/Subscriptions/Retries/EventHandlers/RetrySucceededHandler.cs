using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetrySucceededHandler(ISubscriptionStorage subscriptionStorage) : IShouldNotBeRetried
{
    public Task Handle(RetrySucceeded e, CancellationToken cancellationToken)
    {
        return subscriptionStorage.UnblockCheckpoint(
            new SubscriptionStream(e.SubscriptionName, e.StreamName),
            cancellationToken
        );
    }
}
