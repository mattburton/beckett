namespace Beckett.Subscriptions;

public interface ICheckpointConsumer
{
    void StartPolling();
}
