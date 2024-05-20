using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.Subscriptions.Initialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class BootstrapSubscriptions(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    IMessageTypeMap messageTypeMap,
    BeckettOptions options,
    ISubscriptionInitializer subscriptionInitializer,
    IServiceProvider serviceProvider
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await database.Execute(
            new EnsureCheckpointExists(
                options.ApplicationName,
                GlobalCheckpoint.Name,
                GlobalCheckpoint.StreamName
            ),
            connection,
            cancellationToken
        );

        var checkpoints = new List<CheckpointType>();

        foreach (var subscription in subscriptionRegistry.All())
        {
            EnsureSubscriptionHandlerIsRegistered(subscription);

            subscription.EnsureHandlerIsConfigured();

            subscription.MapMessageTypeNames(messageTypeMap);

            var initialized = await database.Execute(
                new AddOrUpdateSubscription(
                    options.ApplicationName,
                    subscription.Name
                ),
                connection,
                cancellationToken
            );

            if (initialized)
            {
                continue;
            }

            if (subscription.StartingPosition == StartingPosition.Latest)
            {
                await database.Execute(
                    new SetSubscriptionToInitialized(options.ApplicationName, subscription.Name),
                    cancellationToken
                );

                continue;
            }

            checkpoints.Add(
                new CheckpointType
                {
                    Application = options.ApplicationName,
                    Name = subscription.Name,
                    StreamName = InitializationConstants.StreamName,
                    StreamVersion = 0
                }
            );
        }

        if (checkpoints.Count == 0)
        {
            return;
        }

        await database.Execute(new RecordCheckpoints(checkpoints.ToArray()), connection, cancellationToken);

        subscriptionInitializer.Start(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void EnsureSubscriptionHandlerIsRegistered(Subscription subscription)
    {
        if (subscription.HasStaticMethod)
        {
            return;
        }

        try
        {
            var scope = serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService(subscription.Type!);
        }
        catch
        {
            throw new InvalidOperationException(
                $"The subscription handler {subscription.Type} for {subscription.Name} has not been registered in the container"
            );
        }
    }
}
