using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryErrorHandler(IRetryService retryService) : IShouldNotBeRetried
{
    public Task Handle(RetryError e, CancellationToken cancellationToken)
    {
        return retryService.Retry(
            e.SubscriptionName,
            e.StreamName,
            e.StreamPosition,
            e.Attempts,
            e.Exception,
            cancellationToken
        );
    }
}
