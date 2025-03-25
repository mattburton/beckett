namespace Beckett.Subscriptions;

public interface ICheckpointConsumerGroup
{
    void Notify(string groupName);
    Task Poll(CancellationToken stoppingToken);
}
