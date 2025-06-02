using Beckett.Database.Notifications;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.NotificationHandlers;

public class CheckpointNotificationHandler(
    ICheckpointNotificationChannel channel,
    ILogger<CheckpointNotificationHandler> logger
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:checkpoints";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        try
        {
            logger.StartingCheckpointNotificationPolling();

            channel.Notify(payload);
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
