using System.Threading.Channels;
using Beckett.Database;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumerGroup(
    BeckettOptions options,
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    ILogger<CheckpointConsumer> logger
) : ICheckpointConsumerGroup
{
    private readonly Channel<CheckpointAvailable> _channel = Channel.CreateBounded<CheckpointAvailable>(
        options.Subscriptions.GetConcurrency() * 2
    );

    public void Notify(string groupName)
    {
        if (options.Subscriptions.GroupName != groupName)
        {
            return;
        }

        _channel.Writer.TryWrite(CheckpointAvailable.Instance);
    }

    public async Task Poll(CancellationToken stoppingToken)
    {
        var tasks = Enumerable.Range(1, options.Subscriptions.GetConcurrency()).Select(
            x => new CheckpointConsumer(
                database,
                checkpointProcessor,
                options,
                logger
            ).Poll(x, _channel, stoppingToken)
        ).ToArray();

        stoppingToken.Register(() => _channel.Writer.Complete());

        await Task.WhenAll(tasks);
    }
}

public readonly struct CheckpointAvailable
{
    public static CheckpointAvailable Instance { get; } = new();
}
