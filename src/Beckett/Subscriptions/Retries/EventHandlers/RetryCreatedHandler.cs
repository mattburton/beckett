using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryCreatedHandler(IEventStore eventStore)
{
    public Task Handle(RetryCreated e, CancellationToken cancellationToken)
    {
        var retryAt = 0.GetNextDelayWithExponentialBackoff();

        return eventStore.AppendToStream(
            RetryStreamName.For(e.SubscriptionName, e.StreamName, e.StreamPosition),
            ExpectedVersion.StreamExists,
            new RetryScheduled(
                e.SubscriptionName,
                e.StreamName,
                e.StreamPosition,
                0,
                retryAt,
                DateTimeOffset.UtcNow
            ).ScheduleAt(retryAt),
            cancellationToken
        );
    }
}
