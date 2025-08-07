using System.Threading.Channels;
using Beckett.Database;
using Beckett.Storage;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Initialization;

public class GroupSubscriptionInitializer(
    SubscriptionGroup group,
    Channel<UninitializedSubscriptionAvailable> channel,
    IPostgresDatabase database,
    IPostgresDataSource dataSource,
    IMessageStorage messageStorage,
    ILoggerFactory loggerFactory
)
{
    public async Task Initialize(CancellationToken stoppingToken)
    {
        var tasks = Enumerable.Range(1, group.InitializationConcurrency).Select(
            _ => new SubscriptionInitializer(
                group,
                channel,
                database,
                dataSource,
                messageStorage,
                loggerFactory.CreateLogger<SubscriptionInitializer>()
            ).Initialize(stoppingToken)
        ).ToArray();

        await Task.WhenAll(tasks);
    }
}
