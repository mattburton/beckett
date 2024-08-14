using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.Subscriptions.Initialization;
using Beckett.Subscriptions.Queries;
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
        try
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await database.Execute(
                new EnsureCheckpointExists(
                    options.Subscriptions.GroupName,
                    GlobalCheckpoint.Name,
                    GlobalCheckpoint.StreamName
                ),
                connection,
                transaction,
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
                        options.Subscriptions.GroupName,
                        subscription.Name
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                if (initialized)
                {
                    continue;
                }

                if (subscription.StartingPosition == StartingPosition.Latest)
                {
                    await database.Execute(
                        new SetSubscriptionToInitialized(options.Subscriptions.GroupName, subscription.Name),
                        connection,
                        transaction,
                        cancellationToken
                    );

                    continue;
                }

                checkpoints.Add(
                    new CheckpointType
                    {
                        GroupName = options.Subscriptions.GroupName,
                        Name = subscription.Name,
                        StreamName = InitializationConstants.StreamName,
                        StreamVersion = 0
                    }
                );
            }

            if (checkpoints.Count > 0)
            {
                await database.Execute(
                    new RecordCheckpoints(checkpoints.ToArray()),
                    connection,
                    transaction,
                    cancellationToken
                );
            }

            await transaction.CommitAsync(cancellationToken);

            subscriptionInitializer.Start(cancellationToken);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            // do nothing
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void EnsureSubscriptionHandlerIsRegistered(Subscription subscription)
    {
        if (subscription.HandlerType == null)
        {
            return;
        }

        try
        {
            var scope = serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService(subscription.HandlerType);
        }
        catch
        {
            throw new InvalidOperationException(
                $"The subscription handler {subscription.HandlerType} for {subscription.Name} has not been registered in the container"
            );
        }
    }
}
