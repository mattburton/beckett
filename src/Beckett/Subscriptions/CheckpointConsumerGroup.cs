using System.Threading.Channels;
using Beckett.Database;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumerGroup(
    SubscriptionGroup group,
    BeckettOptions options,
    Channel<CheckpointAvailable> channel,
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    ILoggerFactory loggerFactory
)
{
    public async Task Poll(CancellationToken stoppingToken)
    {
        var tasks = Enumerable.Range(1, group.GetConcurrency()).Select(
            x => Task.Run(() => new CheckpointConsumer(
                group,
                channel,
                database,
                checkpointProcessor,
                options,
                loggerFactory.CreateLogger<CheckpointConsumer>()
            ).Poll(x, stoppingToken), stoppingToken)
        ).ToArray();

        await Task.WhenAll(tasks);
    }
}

public readonly struct CheckpointAvailable
{
    public static CheckpointAvailable Instance { get; } = new();
}
