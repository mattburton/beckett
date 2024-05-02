namespace Beckett.Subscriptions;

internal interface ISubscriptionProcessor
{
    void Initialize(CancellationToken stoppingToken);
    void Poll(CancellationToken cancellationToken);
}
