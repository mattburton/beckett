namespace Beckett.Subscriptions;

public interface ISubscriptionStreamConsumerGroup
{
    void Initialize(CancellationToken stoppingToken);

    void StartPolling(string groupName);
}
