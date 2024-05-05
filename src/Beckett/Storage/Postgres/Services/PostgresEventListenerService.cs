using Beckett.Subscriptions;
using Microsoft.Extensions.Hosting;

namespace Beckett.Storage.Postgres.Services;

public class PostgresEventListenerService(
    IPostgresNotificationListener listener,
    ISubscriptionProcessor processor
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return listener.Listen(
            "beckett:events",
            (_, _) => processor.Poll(stoppingToken),
            stoppingToken
        );
    }
}
