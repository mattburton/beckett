using Beckett.Subscriptions;
using Microsoft.Extensions.Hosting;

namespace Beckett.Storage.Postgres.Services;

public class PostgresListenerService(
    IPostgresNotificationListener listener,
    ISubscriptionProcessor processor
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return listener.Listen(
            "beckett:poll",
            (_, _) => processor.Poll(stoppingToken),
            stoppingToken
        );
    }
}
