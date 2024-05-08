using Beckett.Subscriptions;

namespace Beckett.Database.Notifications.Handlers;

public class MessageNotificationHandler(IGlobalStreamConsumer globalStreamConsumer) : IPostgresNotificationHandler
{
    public string Channel => "beckett:messages";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        globalStreamConsumer.Run(cancellationToken);
    }
}
