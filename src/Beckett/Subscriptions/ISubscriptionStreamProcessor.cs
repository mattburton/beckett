namespace Beckett.Subscriptions;

public interface ISubscriptionStreamProcessor
{
    Task Process(
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        bool isRetry,
        CancellationToken cancellationToken
    );
}
