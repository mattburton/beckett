using Beckett.Subscriptions;

namespace Beckett.Database.Notifications.Handlers;

public class CheckpointNotificationHandler(
    ISubscriptionStreamConsumerGroup subscriptionStreamConsumerGroup
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken) =>
        subscriptionStreamConsumerGroup.StartPolling(payload);
}
