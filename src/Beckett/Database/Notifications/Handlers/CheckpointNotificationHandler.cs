using Beckett.Subscriptions;

namespace Beckett.Database.Notifications.Handlers;

public class CheckpointNotificationHandler(
    ISubscriptionConsumerGroup subscriptionConsumerGroup
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken) => subscriptionConsumerGroup.StartPolling();
}
