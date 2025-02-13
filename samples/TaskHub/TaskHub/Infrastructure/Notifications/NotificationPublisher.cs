namespace TaskHub.Infrastructure.Notifications;

public class NotificationPublisher(IMessageStore messageStore) : INotificationPublisher
{
    public Task Publish<T>(
        string streamName,
        T notification,
        CancellationToken cancellationToken
    ) where T : INotification
    {
        return messageStore.AppendToStream(
            $"notifications-{streamName}",
            ExpectedVersion.Any,
            [new StreamTruncated(), notification],
            cancellationToken
        );
    }
}
