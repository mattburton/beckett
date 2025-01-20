using System.Collections.Concurrent;
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

    private readonly int[] _instances = Instances(options);

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly ConcurrentDictionary<int, Task> _consumers = new();

    public void Initialize(CancellationToken stoppingToken)
    {
        foreach (var instance in _instances)
        {
            _consumers.GetOrAdd(
                instance,
                Task.Run(
                    () => new CheckpointConsumer(
                        _channel,
                        instance,
                        database,
                        checkpointProcessor,
                        options,
                        logger
                    ).StartPolling(stoppingToken),
                    stoppingToken
                )
            );
        }

        stoppingToken.Register(() => _channel.Writer.Complete());
    }

    public void StartPolling(string groupName)
    {
        if (options.Subscriptions.GroupName != groupName)
        {
            return;
        }

        _channel.Writer.TryWrite(CheckpointAvailable.Instance);
    }

    private static int[] Instances(BeckettOptions options)
    {
        return Enumerable.Range(1, options.Subscriptions.GetConcurrency()).ToArray();
    }
}

public readonly struct CheckpointAvailable
{
    public static CheckpointAvailable Instance { get; } = new();
}
