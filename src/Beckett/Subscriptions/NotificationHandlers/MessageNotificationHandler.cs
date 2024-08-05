using Beckett.Database.Notifications;

namespace Beckett.Subscriptions.NotificationHandlers;

public class MessageNotificationHandler(IGlobalStreamConsumer globalStreamConsumer) : IPostgresNotificationHandler
{
    public string Channel => "beckett:messages";

    public void Handle(string payload, CancellationToken cancellationToken) =>
        globalStreamConsumer.StartPolling(cancellationToken);
}
