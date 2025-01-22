namespace TaskHub.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task Publish(string channel, object notification, CancellationToken cancellationToken);
}
