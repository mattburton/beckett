namespace Beckett.Database.Notifications;

public interface IPostgresNotificationListener
{
    Task Listen(CancellationToken stoppingToken);
}
