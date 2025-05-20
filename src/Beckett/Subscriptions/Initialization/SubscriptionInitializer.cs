using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.MessageStorage;
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
        while (!cancellationToken.IsCancellationRequested)
        {
            var subscriptionName = await database.Execute(
                new GetNextUninitializedSubscription(options.Subscriptions.GroupName, options.Postgres),
                cancellationToken
            );

            if (subscriptionName == null)
            {
                break;
            }

            var subscription = SubscriptionRegistry.GetSubscription(subscriptionName);

            if (subscription == null)
            {
                logger.LogWarning("Uninitialized subscription {SubscriptionName} does not exist - setting status to 'unknown'", subscriptionName);

                await database.Execute(
                    new SetSubscriptionStatus(
                        options.Subscriptions.GroupName,
                        subscriptionName,
                        SubscriptionStatus.Unknown,
                        options.Postgres
                    ),
                    cancellationToken
                );

                continue;
            }

            var advisoryLockKey = subscription.GetAdvisoryLockKey(options.Subscriptions.GroupName);

            var locked = await database.Execute(
                new TryAdvisoryLock(advisoryLockKey, options.Postgres),
                cancellationToken
            );

            if (!locked)
            {
                continue;
            }

            try
            {
                await InitializeSubscription(subscription, cancellationToken);
            }
            finally
            {
                await database.Execute(new AdvisoryUnlock(advisoryLockKey, options.Postgres), cancellationToken);
            }
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
                    options.Subscriptions.GroupName,
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
                    BatchSize = options.Subscriptions.InitializationBatchSize,
                    Category = subscription.Category,
                    Types = subscription.MessageTypeNames.ToArray()
                },
                cancellationToken
            );

            if (batch.Items.Count == 0)
            {
                var globalStreamCheck = await database.Execute(
                    new LockCheckpoint(
                        options.Subscriptions.GroupName,
                        GlobalCheckpoint.Name,
                        GlobalCheckpoint.StreamName,
                        options.Postgres
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                if (globalStreamCheck == null)
                {
                    continue;
                }

                var nextBatch = await messageStorage.ReadIndexBatch(
                    new ReadIndexBatchOptions
                    {
                        StartingGlobalPosition = checkpoint.StreamPosition,
                        BatchSize = 1,
                        Category = subscription.Category,
                        Types = subscription.MessageTypeNames.ToArray()
                    },
                    cancellationToken
                );

                if (nextBatch.Items.Any())
                {
                    continue;
                }

                await database.Execute(
                    new SetSubscriptionToActive(options.Subscriptions.GroupName, subscription.Name, options.Postgres),
                    connection,
                    transaction,
                    cancellationToken
                );

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

                checkpoints.Add(
                    new CheckpointType
                    {
                        GroupName = options.Subscriptions.GroupName,
                        Name = subscription.Name,
                        StreamName = stream.Key,
                        StreamVersion = stream.Max(x => x.StreamPosition)
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
