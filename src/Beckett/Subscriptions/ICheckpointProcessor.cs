namespace Beckett.Subscriptions;

public interface ICheckpointProcessor
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
