using Beckett.Subscriptions.Retries;

namespace Beckett.Database.Notifications.Handlers;

public class RetryNotificationHandler(IRetryMonitor retryMonitor) : IPostgresNotificationHandler
{
    public string Channel => "beckett:retries";

    public void Handle(string payload, CancellationToken cancellationToken) =>
        retryMonitor.StartPolling(cancellationToken);
}
