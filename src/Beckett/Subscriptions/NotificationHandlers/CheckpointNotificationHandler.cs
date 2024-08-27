using Beckett.Database.Notifications;

namespace Beckett.Subscriptions.NotificationHandlers;

public class CheckpointNotificationHandler(
    ICheckpointConsumerGroup checkpointConsumerGroup
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken) =>
        checkpointConsumerGroup.StartPolling(payload);
}
