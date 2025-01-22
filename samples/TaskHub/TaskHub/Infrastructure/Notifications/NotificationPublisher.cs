namespace TaskHub.Infrastructure.Notifications;

public class NotificationPublisher(IMessageStore messageStore) : INotificationPublisher
{
    public Task Publish(string channel, object notification, CancellationToken cancellationToken)
    {
        return messageStore.AppendToStream(
            $"notifications-{channel}",
            ExpectedVersion.Any,
            notification,
            cancellationToken
        );
    }
}
