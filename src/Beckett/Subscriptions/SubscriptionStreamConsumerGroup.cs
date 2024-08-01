using System.Collections.Concurrent;
using System.Diagnostics;
using Beckett.Database;

namespace Beckett.Subscriptions;

public class SubscriptionStreamConsumerGroup(
    BeckettOptions options,
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionStreamProcessor subscriptionStreamProcessor
) : ISubscriptionStreamConsumerGroup
{
    private readonly int _concurrency = Debugger.IsAttached ? 1 : options.Subscriptions.Concurrency;
    private readonly ConcurrentDictionary<int, ISubscriptionStreamConsumer> _consumers = new();
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
                new SubscriptionStreamConsumer(
                    database,
                    subscriptionRegistry,
                    subscriptionStreamProcessor,
                    options,
                    _stoppingToken ?? default
                )
            );

            consumer.StartPolling();
        }
    }
}
