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
        var tasks = options.Subscriptions.Groups.Select(x => ExecuteForSubscriptionGroup(x, stoppingToken)).ToArray();

        await Task.WhenAll(tasks);

        if (options.Subscriptions.InitializationConcurrency <= 0)
        {
            return;
        }

        await Task.Yield();

        var initializationTasks = Enumerable.Range(1, options.Subscriptions.InitializationConcurrency)
            .Select(_ => subscriptionInitializer.Initialize(stoppingToken)).ToArray();

        await Task.WhenAll(initializationTasks);
    }

    private async Task ExecuteForSubscriptionGroup(SubscriptionGroup group, CancellationToken stoppingToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(stoppingToken);

        await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

        await database.Execute(
            new EnsureCheckpointExists(
                group.Name,
                GlobalCheckpoint.Name,
                GlobalCheckpoint.StreamName
            ),
            connection,
            transaction,
            stoppingToken
        );

        var checkpoints = new List<CheckpointType>();

        foreach (var subscription in group.GetSubscriptions())
        {
            subscription.BuildHandler();

            logger.LogTrace(
                "Adding or updating subscription {Name} in group {GroupName}",
                subscription.Name,
                group.Name
            );

            var status = await database.Execute(
                new AddOrUpdateSubscription(
                    group.Name,
                    subscription.Name
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
                    group.Name
                );

                continue;
            }

            if (status == SubscriptionStatus.Replay)
            {
                logger.LogTrace(
                    "Subscription {Name} in group {GroupName} is currently being replayed - no need for further action.",
                    subscription.Name,
                    group.Name
                );

                continue;
            }

            subscription.Status = status;

            if (subscription.StartingPosition == StartingPosition.Latest &&
                subscription.Status != SubscriptionStatus.Backfill)
            {
                logger.LogTrace(
                    "Subscription {Name} in group {GroupName} has a starting position of {StartingPosition} so it will be set to active and will start processing new messages going forward.",
                    subscription.Name,
                    group.Name,
                    subscription.StartingPosition
                );

                await database.Execute(
                    new SetSubscriptionToActive(
                        group.Name,
                        subscription.Name
                    ),
                    connection,
                    transaction,
                    stoppingToken
                );

                continue;
            }

            logger.LogTrace(
                "Subscription {Name} in group {GroupName} has a starting position of {StartingPosition} or is being backfilled so it will need to be initialized before it can start processing new messages.",
                subscription.Name,
                group.Name,
                subscription.StartingPosition
            );

            checkpoints.Add(
                new CheckpointType
                {
                    GroupName = group.Name,
                    Name = subscription.Name,
                    StreamName = InitializationConstants.StreamName
                }
            );
        }

        if (checkpoints.Count > 0)
        {
            await database.Execute(
                new RecordCheckpoints(checkpoints.ToArray()),
                connection,
                transaction,
                stoppingToken
            );
        }

        await transaction.CommitAsync(stoppingToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
