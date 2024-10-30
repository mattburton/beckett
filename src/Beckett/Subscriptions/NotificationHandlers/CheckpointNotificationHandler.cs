using Beckett.Database.Notifications;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.NotificationHandlers;

public class CheckpointNotificationHandler(
    ICheckpointConsumerGroup checkpointConsumerGroup,
    ILogger<CheckpointNotificationHandler> logger
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        try
        {
            checkpointConsumerGroup.StartPolling(payload);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling checkpoint notification");
        }
    }
}
