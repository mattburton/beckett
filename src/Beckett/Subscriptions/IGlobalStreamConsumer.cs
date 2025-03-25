namespace Beckett.Subscriptions;

public interface IGlobalStreamConsumer
{
    void Notify();
    Task Poll(CancellationToken stoppingToken);
}
