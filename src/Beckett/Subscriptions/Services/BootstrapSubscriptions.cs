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
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(stoppingToken);

        await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

        var globalPosition = await database.Execute(
            new EnsureCheckpointExists(
                options.Subscriptions.GroupName,
                GlobalCheckpoint.Name,
                GlobalCheckpoint.StreamName,
                options.Postgres
            ),
            connection,
            transaction,
            stoppingToken
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

            var status = await database.Execute(
                new AddOrUpdateSubscription(
                    options.Subscriptions.GroupName,
                    subscription.Name,
                    options.Postgres
                ),
                connection,
                transaction,
                stoppingToken
            );

            if (status == SubscriptionStatus.Active)
            {
                logger.LogTrace(
                    "Subscription {Name} in group {GroupName} is already active - no need for further action.",
                    subscription.Name,
                    options.Subscriptions.GroupName
                );

                continue;
            }

            if (subscription.StreamScope == StreamScope.GlobalStream)
            {
                logger.LogTrace(
                    "Subscription {Name} in group {GroupName} is scoped to the global stream so it will be set to active and will start processing messages from the beginning of the global stream.",
                    subscription.Name,
                    options.Subscriptions.GroupName
                );

                checkpoints.Add(
                    new CheckpointType
                    {
                        GroupName = options.Subscriptions.GroupName,
                        Name = subscription.Name,
                        StreamName = GlobalStream.Name,
                        StreamVersion = globalPosition
                    }
                );

                await database.Execute(
                    new SetSubscriptionToActive(
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        options.Postgres
                    ),
                    connection,
                    transaction,
                    stoppingToken
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
                    new SetSubscriptionToActive(
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        options.Postgres
                    ),
                    connection,
                    transaction,
                    stoppingToken
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
                new RecordCheckpoints(checkpoints.ToArray(), options.Postgres),
                connection,
                transaction,
                stoppingToken
            );
        }

        await transaction.CommitAsync(stoppingToken);

        if (options.Subscriptions.InitializationConcurrency <= 0)
        {
            return;
        }

        await Task.Yield();

        var initializationTasks = Enumerable.Range(1, options.Subscriptions.InitializationConcurrency)
            .Select(_ => subscriptionInitializer.Initialize(stoppingToken)).ToArray();

        await Task.WhenAll(initializationTasks);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
