using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Subscriptions.Initialization;
using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class BootstrapSubscriptions(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    ISubscriptionInitializer subscriptionInitializer
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var checkpoints = new List<CheckpointType>();

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

            if (subscription.StartingPosition == StartingPosition.Latest)
            {
                await database.Execute(new SetSubscriptionToInitialized(subscription.Name), cancellationToken);

                continue;
            }

            checkpoints.Add(new CheckpointType
            {
                 Name = subscription.Name,
                 StreamName = InitializationConstants.StreamName,
                 StreamVersion = 0
            });
        }

        if (checkpoints.Count == 0)
        {
            return;
        }

        await database.Execute(new RecordCheckpoints(checkpoints.ToArray()), connection, cancellationToken);

        subscriptionInitializer.Start(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
