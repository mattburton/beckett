using Beckett.Database.Notifications;
using Beckett.Subscriptions.Initialization;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.NotificationHandlers;

public class SubscriptionResetNotificationHandler(
    ISubscriptionInitializerChannel channel,
    BeckettOptions options,
    ILogger<SubscriptionResetNotificationHandler> logger
) : IPostgresNotificationHandler
{
    public string Channel => "beckett:subscriptions:reset";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        try
        {
            var group = options.Subscriptions.Groups.FirstOrDefault(x => x.Name == payload);

            if (group == null)
            {
                return;
            }

            logger.ReceivedSubscriptionResetNotification(group.Name);

            channel.Notify(group);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error handling subscription reset notification");
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Information, "Received subscription reset notification for group {GroupName}")]
    public static partial void ReceivedSubscriptionResetNotification(this ILogger logger, string groupName);
}
