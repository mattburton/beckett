using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryScheduledHandler(IEventStore eventStore, ISubscriptionProcessor processor, BeckettOptions beckett)
{
    public async Task Handle(RetryScheduled e, CancellationToken cancellationToken)
    {
        try
        {
            await processor.ProcessSubscriptionStreamAtPosition(
                e.SubscriptionName,
                e.StreamName,
                e.StreamPosition,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            var attempts = e.Attempts;
            var currentAttempt = attempts + 1;
            var retryAt = currentAttempt.GetNextDelayWithExponentialBackoff();

            if (currentAttempt >= beckett.Subscriptions.MaxRetryCount)
            {
                await eventStore.AppendToStream(
                    RetryStreamName.For(e.SubscriptionName, e.StreamName, e.StreamPosition),
                    ExpectedVersion.StreamExists,
                    new RetryFailed(
                        e.SubscriptionName,
                        e.StreamName,
                        e.StreamPosition,
                        currentAttempt,
                        ExceptionData.From(ex),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }
            else
            {
                await eventStore.AppendToStream(
                    RetryStreamName.For(e.SubscriptionName, e.StreamName, e.StreamPosition),
                    ExpectedVersion.StreamExists,
                    [
                        new RetryUnsuccessful(
                            e.SubscriptionName,
                            e.StreamName,
                            e.StreamPosition,
                            currentAttempt,
                            ExceptionData.From(ex),
                            DateTimeOffset.UtcNow
                        ),
                        (e with
                        {
                            Attempts = currentAttempt,
                            RetryAt = retryAt,
                            Timestamp = DateTimeOffset.UtcNow
                        }).ScheduleAt(retryAt)
                    ],
                    cancellationToken
                );
            }
        }
    }
}
