using Beckett.Subscriptions;

namespace Beckett.Database.Notifications.Handlers;

public class EventsNotificationHandler(IGlobalStreamConsumer globalStreamConsumer) : IPostgresNotificationHandler
{
    public string Channel => "beckett:events";

    public void Handle(string payload, CancellationToken cancellationToken)
    {
        globalStreamConsumer.Run(cancellationToken);
    }
}
