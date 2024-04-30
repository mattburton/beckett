namespace Beckett.Subscriptions;

public interface ISubscriptionStreamProcessor
{
    void Initialize(CancellationToken stoppingToken);
    void StartPolling(CancellationToken cancellationToken);
    Task Poll(CancellationToken cancellationToken);
}
