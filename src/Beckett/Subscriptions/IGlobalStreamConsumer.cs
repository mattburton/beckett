namespace Beckett.Subscriptions;

public interface IGlobalStreamConsumer
{
    void StartPolling(CancellationToken cancellationToken);
}
