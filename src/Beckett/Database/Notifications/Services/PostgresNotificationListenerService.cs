using Microsoft.Extensions.Hosting;

namespace Beckett.Database.Notifications.Services;

public class PostgresNotificationListenerService(IPostgresNotificationListener listener) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => listener.Listen(stoppingToken);
}
