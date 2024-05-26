namespace Beckett.Subscriptions.Retries;

public interface IRetryMonitor
{
    void StartPolling(CancellationToken stoppingToken);
}
