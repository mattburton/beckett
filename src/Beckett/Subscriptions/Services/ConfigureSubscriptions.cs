using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class ConfigureSubscriptions(ISubscriptionRegistry subscriptionRegistry, ISubscriptionStorage subscriptionStorage) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptionRegistry.All())
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
