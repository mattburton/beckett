using Beckett.Subscriptions;

namespace Beckett.Database.Notifications.Handlers;

public class CheckpointsNotificationHandler(ISubscriptionConsumerGroup consumerGroup) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        consumerGroup.StartPolling();
    }
}
