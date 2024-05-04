namespace Beckett.Subscriptions;

public interface ISubscriptionConsumerGroup
{
    void Initialize(CancellationToken stoppingToken);

    void StartPolling();
}
