using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class ConfigureSubscriptions(ISubscriptionStorage subscriptionStorage) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
