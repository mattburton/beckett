using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class SubscriptionErrorHandler(IRetryService retryService) : IShouldNotBeRetried
{
    public Task Handle(SubscriptionError e, CancellationToken cancellationToken)
    {
        return retryService.Retry(
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            0,
            cancellationToken
        );
    }
}
