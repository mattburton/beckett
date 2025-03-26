namespace Core.Notifications;

public class FakeNotificationPublisher : INotificationPublisher
{
    public PublishedNotification? Received { get; private set; }

    public Task Publish<T>(string streamName, T notification, CancellationToken cancellationToken) where T : INotification
    {
        Received = new PublishedNotification(streamName, notification);

        return Task.CompletedTask;
    }

    public record PublishedNotification(string Channel, object Notification);
}
