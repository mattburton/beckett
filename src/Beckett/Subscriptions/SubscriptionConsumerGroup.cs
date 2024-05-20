using System.Collections.Concurrent;
using System.Diagnostics;
using Beckett.Database;

namespace Beckett.Subscriptions;

public class SubscriptionConsumerGroup(
    BeckettOptions options,
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionStreamProcessor subscriptionStreamProcessor
) : ISubscriptionConsumerGroup
{
    private readonly int _concurrency = Debugger.IsAttached ? 1 : options.Subscriptions.Concurrency;
    private readonly ConcurrentDictionary<int, ISubscriptionConsumer> _consumers = new();
    private CancellationToken? _stoppingToken;

    public void Initialize(CancellationToken stoppingToken) => _stoppingToken = stoppingToken;

    public void StartPolling()
    {
        foreach (var instance in Enumerable.Range(1, _concurrency))
        {
            var consumer = _consumers.GetOrAdd(
                instance,
                new SubscriptionConsumer(
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
