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
            globalStreamConsumer.StartPolling(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling message notification");
        }
    }
}
