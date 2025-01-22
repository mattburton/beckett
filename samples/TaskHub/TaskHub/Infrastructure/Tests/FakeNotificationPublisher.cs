namespace TaskHub.Infrastructure.Tests;

public class FakeNotificationPublisher : INotificationPublisher
{
    public PublishedNotification? Received { get; private set; }

    public Task Publish(string channel, object notification, CancellationToken cancellationToken)
    {
        Received = new PublishedNotification(channel, notification);

        return Task.CompletedTask;
    }

    public record PublishedNotification(string Channel, object Notification);
}
