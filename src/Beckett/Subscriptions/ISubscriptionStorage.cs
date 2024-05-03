namespace Beckett.Subscriptions;

public interface ISubscriptionStorage
{
    Task AddOrUpdateSubscription(
        string subscriptionName,
        string[] eventTypes,
        bool startFromBeginning,
        CancellationToken cancellationToken
    );

    IEnumerable<Task> ConfigureBackgroundService(ISubscriptionProcessor processor, CancellationToken stoppingToken);

    Task<IReadOnlyList<SubscriptionStream>> GetSubscriptionStreamsToProcess(
        int batchSize,
        CancellationToken cancellationToken
    );

    Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        long? fromStreamPosition,
        int batchSize,
        bool retryOnError,
        ProcessSubscriptionStreamCallback callback,
        CancellationToken cancellationToken
    );

    Task UnblockCheckpoint(SubscriptionStream subscriptionStream, CancellationToken cancellationToken);
}

public delegate Task<ProcessSubscriptionStreamResult> ProcessSubscriptionStreamCallback(
    Subscription subscription,
    SubscriptionStream subscriptionStream,
    IReadOnlyList<IEventContext> events,
    bool retryOnError,
    CancellationToken cancellationToken
);

public abstract record ProcessSubscriptionStreamResult
{
    public record NoEvents : ProcessSubscriptionStreamResult;

    public record Skipped : ProcessSubscriptionStreamResult;

    public record Success(long StreamPosition) : ProcessSubscriptionStreamResult;

    public record Blocked(long StreamPosition, Exception Exception) : ProcessSubscriptionStreamResult;

    public record Error : ProcessSubscriptionStreamResult;
}
