using Beckett.Subscriptions;

namespace Beckett.Storage;

public interface IStorageProvider
{
    Task AddOrUpdateSubscription(string subscriptionName, string[] eventTypes, bool startFromBeginning,
        CancellationToken cancellationToken);

    Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    );

    IEnumerable<Task> GetSubscriptionHostTasks(ISubscriptionStreamProcessor processor, CancellationToken stoppingToken);

    Task<IReadOnlyList<SubscriptionStream>> GetSubscriptionStreamsToProcess(int batchSize,
        CancellationToken cancellationToken);

    Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        ProcessSubscriptionStreamCallback callback,
        CancellationToken cancellationToken
    );

    Task<IReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);

    Task<IReadResult> ReadSubscriptionStream(SubscriptionStream subscriptionStream, int batchSize,
        CancellationToken cancellationToken);

    Task RecordCheckpoint(
        SubscriptionStream subscriptionStream,
        long checkpoint,
        bool blocked,
        CancellationToken cancellationToken
    );
}

public delegate Task<ProcessSubscriptionStreamResult> ProcessSubscriptionStreamCallback(
    Subscription subscription,
    SubscriptionStream subscriptionStream,
    IReadOnlyList<EventContext> events,
    CancellationToken cancellationToken
);

public readonly record struct EventContext(
    Guid Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    Type Type,
    object Data,
    IDictionary<string, object> Metadata,
    DateTimeOffset Timestamp
);

public abstract record ProcessSubscriptionStreamResult
{
    public record NoEvents : ProcessSubscriptionStreamResult;

    public record Skipped : ProcessSubscriptionStreamResult;

    public record Success(long StreamPosition) : ProcessSubscriptionStreamResult;

    public record Blocked(long StreamPosition) : ProcessSubscriptionStreamResult;

    public record Error : ProcessSubscriptionStreamResult;
}
