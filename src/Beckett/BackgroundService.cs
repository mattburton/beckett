using Beckett.Events;
using Beckett.Subscriptions;

namespace Beckett;

public class BackgroundService(
    BeckettOptions options,
    IEventStorage eventStorage,
    ISubscriptionStorage subscriptionStorage,
    ISubscriptionProcessor subscriptionProcessor
) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await eventStorage.Initialize(stoppingToken);

        await subscriptionStorage.Initialize(stoppingToken);

        await ConfigureSubscriptions(stoppingToken);

        subscriptionProcessor.Initialize(stoppingToken);

        var tasks = new List<Task>();

        tasks.AddRange(subscriptionStorage.ConfigureServiceHost(subscriptionProcessor, stoppingToken));

        tasks.Add(ContinuousPolling(subscriptionProcessor, options, stoppingToken));

        await Task.WhenAll(tasks);
    }

    private static async Task ContinuousPolling(
        ISubscriptionProcessor subscriptionProcessor,
        BeckettOptions options,
        CancellationToken cancellationToken
    )
    {
        if (options.Subscriptions.PollingInterval == TimeSpan.Zero)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            subscriptionProcessor.Poll(cancellationToken);

            await Task.Delay(options.Subscriptions.PollingInterval, cancellationToken);
        }
    }

    private async Task ConfigureSubscriptions(CancellationToken cancellationToken)
    {
        foreach (var subscription in SubscriptionRegistry.All())
        {
            await subscriptionStorage.AddOrUpdateSubscription(
                subscription.Name,
                subscription.EventTypes,
                subscription.StartingPosition == StartingPosition.Earliest,
                cancellationToken
            );
        }
    }
}
