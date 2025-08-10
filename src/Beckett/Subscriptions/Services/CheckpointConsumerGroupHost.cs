using Beckett.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class CheckpointConsumerGroupHost(
    BeckettOptions options,
    ICheckpointNotificationChannel channel,
    IPostgresDatabase database,
    ICheckpointProcessor processor,
    ISubscriptionRegistry registry,
    ILoggerFactory loggerFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerGroups = options.Subscriptions.Groups.Select(BuildConsumerGroupForSubscriptionGroup);

        var tasks = consumerGroups.Select(x => x.Poll(stoppingToken)).ToArray();

        await Task.WhenAll(tasks);
    }

    private CheckpointConsumerGroup BuildConsumerGroupForSubscriptionGroup(SubscriptionGroup group)
    {
        return new CheckpointConsumerGroup(group, options, channel.For(group.Name), database, processor, registry, loggerFactory);
    }
}
