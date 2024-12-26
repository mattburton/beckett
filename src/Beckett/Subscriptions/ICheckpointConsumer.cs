namespace Beckett.Subscriptions;

public interface ICheckpointConsumer
{
    Task StartPolling(CancellationToken cancellationToken);
}
