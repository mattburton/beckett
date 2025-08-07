using Beckett.Database;
using Beckett.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Initialization;

public class GroupSubscriptionInitializerHost(
    BeckettOptions options,
    ISubscriptionInitializerChannel channel,
    IPostgresDatabase database,
    IPostgresDataSource dataSource,
    IMessageStorage messageStorage,
    ILoggerFactory loggerFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Subscriptions.InitializationEnabled)
        {
            return;
        }

        var groupSubscriptionInitializers = options.Subscriptions.Groups.Select(Build);

        var tasks = groupSubscriptionInitializers.Select(x => x.Initialize(stoppingToken)).ToArray();

        await Task.WhenAll(tasks);
    }

    private GroupSubscriptionInitializer Build(SubscriptionGroup group)
    {
        return new GroupSubscriptionInitializer(
            group,
            channel.For(group),
            database,
            dataSource,
            messageStorage,
            loggerFactory
        );
    }
}
