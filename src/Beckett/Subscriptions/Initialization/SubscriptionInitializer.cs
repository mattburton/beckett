using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Initialization;

public class SubscriptionInitializer(
    IPostgresDatabase database,
    IPostgresDataSource dataSource,
    BeckettOptions options,
    IMessageStorage messageStorage,
    ILogger<SubscriptionInitializer> logger
) : ISubscriptionInitializer
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        var tasks = options.Subscriptions.Groups.Select(x => InitializeSubscriptionsForGroup(x, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task InitializeSubscriptionsForGroup(SubscriptionGroup group, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = dataSource.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            var subscriptionName = await database.Execute(
                new GetNextUninitializedSubscription(group.Name, options.Postgres),
                connection,
                cancellationToken
            );

            if (subscriptionName == null)
            {
                break;
            }

            var subscription = group.GetSubscription(subscriptionName);

            if (subscription == null)
            {
                logger.LogWarning(
                    "Uninitialized subscription {SubscriptionName} does not exist - setting status to 'unknown'",
                    subscriptionName
                );

                await database.Execute(
                    new SetSubscriptionStatus(
                        group.Name,
                        subscriptionName,
                        SubscriptionStatus.Unknown,
                        options.Postgres
                    ),
                    connection,
                    cancellationToken
                );

                continue;
            }

            await InitializeSubscription(subscription, cancellationToken);
        }
    }

    private async Task InitializeSubscription(Subscription subscription, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = dataSource.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(
                    subscription.Group.Name,
                    subscription.Name,
                    InitializationConstants.StreamName,
                    options.Postgres
                ),
                connection,
                transaction,
                cancellationToken
            );

            if (checkpoint == null)
            {
                break;
            }

            logger.InitializingSubscription(subscription.Name);

            var batch = await messageStorage.ReadIndexBatch(
                new ReadIndexBatchOptions
                {
                    StartingGlobalPosition = checkpoint.StreamPosition,
                    BatchSize = options.Subscriptions.InitializationBatchSize
                },
                cancellationToken
            );

            if (batch.Items.Count == 0)
            {
                var globalCheckpoint = await database.Execute(
                    new LockCheckpoint(
                        subscription.Group.Name,
                        GlobalCheckpoint.Name,
                        GlobalCheckpoint.StreamName,
                        options.Postgres
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                if (globalCheckpoint == null)
                {
                    continue;
                }

                var nextBatch = await messageStorage.ReadIndexBatch(
                    new ReadIndexBatchOptions
                    {
                        StartingGlobalPosition = checkpoint.StreamPosition,
                        BatchSize = 1
                    },
                    cancellationToken
                );

                if (nextBatch.Items.Any())
                {
                    continue;
                }

                // subscriptions for a new group can just be activated immediately vs put into replay mode
                if (globalCheckpoint.StreamPosition == 0 || subscription.SkipDuringReplay)
                {
                    await database.Execute(
                        new SetSubscriptionToActive(
                            subscription.Group.Name,
                            subscription.Name,
                            options.Postgres
                        ),
                        connection,
                        transaction,
                        cancellationToken
                    );
                }
                else
                {
                    await database.Execute(
                        new SetSubscriptionToReplay(
                            subscription.Group.Name,
                            subscription.Name,
                            globalCheckpoint.StreamPosition,
                            options.Postgres
                        ),
                        connection,
                        transaction,
                        cancellationToken
                    );
                }

                await transaction.CommitAsync(cancellationToken);

                logger.FinishedInitializingSubscription(subscription.Name);

                break;
            }

            var checkpoints = new List<CheckpointType>();

            foreach (var stream in batch.Items.GroupBy(x => x.StreamName))
            {
                if (stream.All(x => !x.AppliesTo(subscription)))
                {
                    continue;
                }

                var streamVersion = stream.Max(x => x.StreamPosition);

                // if the subscription is set to skip during replay automatically advance the stream position
                var streamPosition = subscription.SkipDuringReplay ? streamVersion : 0;

                checkpoints.Add(
                    new CheckpointType
                    {
                        GroupName = subscription.Group.Name,
                        Name = subscription.Name,
                        StreamName = stream.Key,
                        StreamPosition = streamPosition,
                        StreamVersion = streamVersion
                    }
                );
            }

            await database.Execute(
                new RecordCheckpoints(checkpoints.ToArray(), options.Postgres),
                connection,
                transaction,
                cancellationToken
            );

            var newGlobalPosition = batch.Items.Max(x => x.GlobalPosition);

            await database.Execute(
                new UpdateSystemCheckpointPosition(
                    checkpoint.Id,
                    newGlobalPosition,
                    options.Postgres
                ),
                connection,
                transaction,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            logger.SubscriptionInitializationPosition(subscription.Name, newGlobalPosition);
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Information, "Initializing subscription {SubscriptionName}")]
    public static partial void InitializingSubscription(this ILogger logger, string subscriptionName);

    [LoggerMessage(0, LogLevel.Information, "Finished initializing subscription {SubscriptionName}")]
    public static partial void FinishedInitializingSubscription(this ILogger logger, string subscriptionName);

    [LoggerMessage(
        0,
        LogLevel.Information,
        "Initializing subscription {SubscriptionName} - current global position {GlobalPosition}"
    )]
    public static partial void SubscriptionInitializationPosition(
        this ILogger logger,
        string subscriptionName,
        long globalPosition
    );
}
