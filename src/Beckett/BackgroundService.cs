using Beckett.Events;
using Beckett.Subscriptions;

namespace Beckett;

public class BackgroundService(
    BeckettOptions beckett,
    IEventStorage eventStorage,
    ISubscriptionStorage subscriptionStorage,
    ISubscriptionProcessor subscriptionProcessor
) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConfigureSubscriptions(stoppingToken);

        subscriptionProcessor.Initialize(stoppingToken);

        var tasks = new List<Task>();

        tasks.AddRange(eventStorage.ConfigureBackgroundService(stoppingToken));

        tasks.AddRange(subscriptionStorage.ConfigureBackgroundService(subscriptionProcessor, stoppingToken));

        tasks.Add(ContinuousPolling(subscriptionProcessor, beckett, stoppingToken));

        await Task.WhenAll(tasks);
    }

    private static async Task ContinuousPolling(
        ISubscriptionProcessor subscriptionProcessor,
        BeckettOptions beckett,
        CancellationToken cancellationToken
    )
    {
        if (beckett.Subscriptions.PollingInterval == TimeSpan.Zero)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            subscriptionProcessor.Poll(cancellationToken);

            await Task.Delay(beckett.Subscriptions.PollingInterval, cancellationToken);
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
