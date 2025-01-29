namespace Beckett.Subscriptions;

public interface ICheckpointConsumerGroup
{
    void Initialize(CancellationToken stoppingToken);

    void StartPolling(string payload);
}
