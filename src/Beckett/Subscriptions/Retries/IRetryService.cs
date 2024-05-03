using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Retries;

public interface IRetryService
{
    Task Retry(string subscriptionName, string streamName, long streamPosition, CancellationToken cancellationToken);
}

public class RetryService(IEventStore eventStore, ILogger<RetryService> logger) : IRetryService
{
    public async Task Retry(
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await eventStore.AppendToStream(
                RetryStreamName.For(subscriptionName, streamName, streamPosition),
                ExpectedVersion.StreamDoesNotExist,
                new RetryCreated(subscriptionName, streamName, streamPosition, DateTimeOffset.UtcNow),
                cancellationToken
            );
        }
        catch (StreamAlreadyExistsException)
        {
            logger.LogWarning(
                "Attempted to create a retry where one is already in progress - subscription: {SubscriptionName}, stream name: {StreamName}, stream position: {StreamPosition}",
                subscriptionName,
                streamName,
                streamPosition
            );
        }
    }
}
