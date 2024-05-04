using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries;

public interface IRetryService
{
    Task Retry(
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        ExceptionData exception,
        CancellationToken cancellationToken
    );
}

public class RetryService(
    BeckettOptions options,
    ISubscriptionProcessor processor,
    IEventStore eventStore
) : IRetryService
{
    public async Task Retry(
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        ExceptionData exception,
        CancellationToken cancellationToken
    )
    {
        var retryStreamName = RetryStreamName.For(subscriptionName, streamName, streamPosition);
        var retryAt = attempts.GetNextDelayWithExponentialBackoff();

        try
        {
            await processor.ProcessSubscriptionStreamAtPosition(
                subscriptionName,
                streamName,
                streamPosition,
                cancellationToken
            );

            await eventStore.AppendToStream(
                retryStreamName,
                ExpectedVersion.StreamExists,
                new RetrySucceeded(
                    subscriptionName,
                    streamName,
                    streamPosition,
                    attempts + 1,
                    DateTimeOffset.UtcNow
                ),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            if (attempts >= options.Subscriptions.MaxRetryCount)
            {
                await eventStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new RetryFailed(
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attempts + 1,
                        ExceptionData.From(ex),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }
            else
            {
                await eventStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new RetryError(
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attempts + 1,
                        ExceptionData.From(ex),
                        retryAt,
                        DateTimeOffset.UtcNow
                    ).ScheduleAt(retryAt),
                    cancellationToken
                );
            }
        }
    }
}
