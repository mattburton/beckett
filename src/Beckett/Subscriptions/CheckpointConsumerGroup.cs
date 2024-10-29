using System.Collections.Concurrent;
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
    private readonly int[] _instances = Instances(options);
    private readonly ConcurrentDictionary<int, ICheckpointConsumer> _consumers = new();
    private CancellationToken? _stoppingToken;

    public void Initialize(CancellationToken stoppingToken) => _stoppingToken = stoppingToken;

    public void StartPolling(string groupName)
    {
        if (options.Subscriptions.GroupName != groupName)
        {
            return;
        }

        foreach (var instance in _instances)
        {
            var consumer = _consumers.GetOrAdd(
                instance,
                new CheckpointConsumer(
                    instance,
                    database,
                    checkpointProcessor,
                    options,
                    logger,
                    _stoppingToken ?? default
                )
            );

            consumer.StartPolling();
        }
    }

    private static int[] Instances(BeckettOptions options)
    {
        return Enumerable.Range(1, options.Subscriptions.GetConcurrency()).ToArray();
    }
}
