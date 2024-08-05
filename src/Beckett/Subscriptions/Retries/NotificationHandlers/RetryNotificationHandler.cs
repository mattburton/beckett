using Beckett.Database.Notifications;

namespace Beckett.Subscriptions.Retries.NotificationHandlers;

public class RetryNotificationHandler(IRetryMonitor retryMonitor) : IPostgresNotificationHandler
{
    public string Channel => "beckett:retries";

    public void Handle(string payload, CancellationToken cancellationToken) =>
        retryMonitor.StartPolling(cancellationToken);
}
