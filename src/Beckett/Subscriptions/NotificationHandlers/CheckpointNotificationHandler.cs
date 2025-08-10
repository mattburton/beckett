using Beckett.Database.Notifications;
using Beckett.Subscriptions.Services;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.NotificationHandlers;

public class CheckpointNotificationHandler(
    ICheckpointNotificationChannel channel,
    ISubscriptionRegistry registry,
    ILogger<CheckpointNotificationHandler> logger
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        try
        {
            // Payload is now subscription_id, convert to group name
            if (!long.TryParse(payload, out var subscriptionId))
            {
                logger.LogWarning("Invalid subscription_id in checkpoint notification payload: {Payload}", payload);
                return;
            }

            var subscription = registry.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                logger.LogWarning("Unknown subscription_id in checkpoint notification: {SubscriptionId}", subscriptionId);
                return;
            }

            logger.StartingCheckpointNotificationPolling();

            channel.Notify(subscription.Value.GroupName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling checkpoint notification");
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "Checkpoint notification received - starting polling")]
    public static partial void StartingCheckpointNotificationPolling(this ILogger logger);
}
