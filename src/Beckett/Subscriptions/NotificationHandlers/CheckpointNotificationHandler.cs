using Beckett.Database.Notifications;

namespace Beckett.Subscriptions.NotificationHandlers;

public class CheckpointNotificationHandler(
    ISubscriptionStreamConsumerGroup subscriptionStreamConsumerGroup
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken) =>
        subscriptionStreamConsumerGroup.StartPolling(payload);
}
