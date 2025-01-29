using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Subscriptions.Initialization;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class BootstrapSubscriptions(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    BeckettOptions options,
    ISubscriptionInitializer subscriptionInitializer,
    ILogger<BootstrapSubscriptions> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = dataSource.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            options.Subscriptions.GroupId = await database.Execute(
                new GetOrAddGroup(
                    options.Subscriptions.GroupName,
                    options.Postgres
                ),
                connection,
                transaction,
                cancellationToken
            );

            var checkpoints = new List<CheckpointType>();

            foreach (var subscription in SubscriptionRegistry.All())
            {
                subscription.BuildHandler();

                logger.LogTrace(
                    "Adding or updating subscription {Name} in group {GroupName}",
                    subscription.Name,
                    options.Subscriptions.GroupName
                );

                var result = await database.Execute(
                    new GetOrAddSubscription(options.Subscriptions.GroupId, subscription.Name, options.Postgres),
                    connection,
                    transaction,
                    cancellationToken
                );

                subscription.Id = result.Id;

                if (result.Status == SubscriptionStatus.Active)
                {
                    logger.LogTrace(
                        "Subscription {Name} in group {GroupName} is already active - no need for further action.",
                        subscription.Name,
                        options.Subscriptions.GroupName
                    );

                    continue;
                }

                if (subscription.StartingPosition == StartingPosition.Latest)
                {
                    logger.LogTrace(
                        "Subscription {Name} in group {GroupName} has a starting position of {StartingPosition} so it will be set to active and will start processing new messages going forward.",
                        subscription.Name,
                        options.Subscriptions.GroupName,
                        subscription.StartingPosition
                    );

                    await database.Execute(
                        new SetSubscriptionToActive(subscription.Id, options.Postgres),
                        connection,
                        transaction,
                        cancellationToken
                    );

                    continue;
                }

                logger.LogTrace(
                    "Subscription {Name} in group {GroupName} has a starting position of {StartingPosition} so it will need to be initialized before it can start processing new messages.",
                    subscription.Name,
                    options.Subscriptions.GroupName,
                    subscription.StartingPosition
                );

                checkpoints.Add(
                    new CheckpointType
                    {
                        SubscriptionId = subscription.Id,
                        StreamName = InitializationConstants.StreamName,
                        StreamVersion = 0
                    }
                );
            }

            if (checkpoints.Count > 0)
            {
                await database.Execute(
                    new RecordCheckpoints(checkpoints.ToArray(), options.Postgres),
                    connection,
                    transaction,
                    cancellationToken
                );
            }

            await transaction.CommitAsync(cancellationToken);

            if (options.Subscriptions.InitializationConcurrency <= 0)
            {
                return;
            }

            await Task.Yield();

            var initializationTasks = Enumerable.Range(1, options.Subscriptions.InitializationConcurrency)
                .Select(_ => subscriptionInitializer.Initialize(cancellationToken)).ToArray();

            await Task.WhenAll(initializationTasks);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            // do nothing
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
