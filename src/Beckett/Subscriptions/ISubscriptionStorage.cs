using Beckett.Events;

namespace Beckett.Subscriptions;

internal interface ISubscriptionStorage
{
    Task Initialize(CancellationToken cancellationToken);

    Task AddOrUpdateSubscription(
        string subscriptionName,
        string[] eventTypes,
        bool startFromBeginning,
        CancellationToken cancellationToken
    );

    IEnumerable<Task> ConfigureServiceHost(ISubscriptionProcessor processor, CancellationToken stoppingToken);

    Task<IReadOnlyList<SubscriptionStream>> GetSubscriptionStreamsToProcess(
        int batchSize,
        CancellationToken cancellationToken
    );

    Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        int batchSize,
        ProcessSubscriptionStreamCallback callback,
        CancellationToken cancellationToken
    );
}

internal delegate Task<ProcessSubscriptionStreamResult> ProcessSubscriptionStreamCallback(
    Subscription subscription,
    SubscriptionStream subscriptionStream,
    IReadOnlyList<EventData> events,
    CancellationToken cancellationToken
);

internal abstract record ProcessSubscriptionStreamResult
{
    public record NoEvents : ProcessSubscriptionStreamResult;

    public record Skipped : ProcessSubscriptionStreamResult;

    public record Success(long StreamPosition) : ProcessSubscriptionStreamResult;

    public record Blocked(long StreamPosition) : ProcessSubscriptionStreamResult;

    public record Error : ProcessSubscriptionStreamResult;
}
