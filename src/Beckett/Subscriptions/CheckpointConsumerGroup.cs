using System.Collections.Concurrent;
using System.Diagnostics;
using Beckett.Database;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumerGroup(
    BeckettOptions options,
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ICheckpointProcessor checkpointProcessor,
    ILogger<CheckpointConsumer> logger
) : ICheckpointConsumerGroup
{
    private readonly int _concurrency = Debugger.IsAttached ? 1 : options.Subscriptions.Concurrency;
    private readonly ConcurrentDictionary<int, ICheckpointConsumer> _consumers = new();
    private CancellationToken? _stoppingToken;

    public void Initialize(CancellationToken stoppingToken) => _stoppingToken = stoppingToken;

    public void StartPolling(string groupName)
    {
        if (options.Subscriptions.GroupName != groupName)
        {
            return;
        }

        foreach (var instance in Enumerable.Range(1, _concurrency))
        {
            var consumer = _consumers.GetOrAdd(
                instance,
                new CheckpointConsumer(
                    database,
                    subscriptionRegistry,
                    checkpointProcessor,
                    options,
                    logger,
                    _stoppingToken ?? default
                )
            );

            consumer.StartPolling();
        }
    }
}
