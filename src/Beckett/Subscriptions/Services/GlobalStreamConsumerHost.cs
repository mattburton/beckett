using Beckett.Database;
using Beckett.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class GlobalStreamConsumerHost(
    BeckettOptions options,
    IGlobalStreamNotificationChannel channel,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    ILoggerFactory loggerFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumers = options.Subscriptions.Groups.Select(BuildConsumerForSubscriptionGroup);

        var tasks = consumers.Select(x => Task.Run(() => x.Poll(stoppingToken), stoppingToken)).ToArray();

        await Task.WhenAll(tasks);
    }

    private GlobalStreamConsumer BuildConsumerForSubscriptionGroup(SubscriptionGroup group)
    {
        return new GlobalStreamConsumer(
            group,
            channel.For(group.Name),
            dataSource,
            database,
            messageStorage,
            loggerFactory.CreateLogger<GlobalStreamConsumer>()
        );
    }
}
