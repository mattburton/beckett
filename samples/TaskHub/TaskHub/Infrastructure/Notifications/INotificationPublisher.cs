namespace TaskHub.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task Publish<T>(string streamName, T notification, CancellationToken cancellationToken) where T : INotification;
}
