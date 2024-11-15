using Beckett.Database.Notifications;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.NotificationHandlers;

public class MessageNotificationHandler(
    IGlobalStreamConsumer globalStreamConsumer,
    ILogger<MessageNotificationHandler> logger
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:messages";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        try
        {
            logger.StartingGlobalStreamNotificationPolling();

            globalStreamConsumer.StartPolling(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling message notification");
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "Starting global stream notification polling")]
    public static partial void StartingGlobalStreamNotificationPolling(this ILogger logger);
}
