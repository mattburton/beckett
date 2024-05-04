namespace Beckett.Subscriptions;

public interface ISubscriptionProcessor
{
    void Initialize(CancellationToken stoppingToken);

    void Poll(CancellationToken cancellationToken);

    Task ProcessSubscriptionStreamAtPosition(
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    );
}
