using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class InitializeSubscriptions(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        foreach (var subscription in subscriptionRegistry.All())
        {
            var initialized = await database.Execute(
                new AddOrUpdateSubscription(subscription.Name),
                connection,
                cancellationToken
            );

            if (initialized)
            {
                continue;
            }

            //TODO - initialize subscriptions if necessary
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
