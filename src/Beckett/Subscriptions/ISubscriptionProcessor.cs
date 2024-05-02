namespace Beckett.Subscriptions;

public interface ISubscriptionProcessor
{
    void Initialize(CancellationToken stoppingToken);
    void Poll(CancellationToken cancellationToken);
}
