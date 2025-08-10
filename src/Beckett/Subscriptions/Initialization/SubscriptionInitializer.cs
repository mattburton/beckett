using System.Threading.Channels;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Initialization;

public class SubscriptionInitializer(
    SubscriptionGroup group,
    Channel<UninitializedSubscriptionAvailable> channel,
    IPostgresDatabase database,
    IPostgresDataSource dataSource,
    IMessageStorage messageStorage,
    ISubscriptionRegistry registry,
    ILogger<SubscriptionInitializer> logger
)
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (channel.Reader.TryRead(out _))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await using var connection = dataSource.CreateConnection();

                        await connection.OpenAsync(cancellationToken);

                        var subscriptionName = await database.Execute(
                            new GetNextUninitializedSubscription(group.Name),
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

                            var subscriptionId = registry.GetSubscriptionId(group.Name, subscriptionName);

                            if (subscriptionId.HasValue)
                            {
                                await database.Execute(
                                    new SetSubscriptionStatus(
                                        subscriptionId.Value,
                                        SubscriptionStatus.Unknown
                                    ),
                                connection,
                                cancellationToken
                                );
                            }

                            continue;
                        }

                        await InitializeSubscription(subscription, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(
                            e,
                            "Unhandled exception while initializing subscriptions - will try again in 10 seconds"
                        );

                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }
                }
            }
        }
    }

    private async Task InitializeSubscription(Subscription subscription, CancellationToken cancellationToken)
    {
        long? replayTargetPosition = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = dataSource.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var subscriptionId = registry.GetSubscriptionId(subscription.Group.Name, subscription.Name);

            if (subscriptionId == null)
            {
                logger.LogWarning("Subscription {SubscriptionName} in group {GroupName} not found in mapping",
                    subscription.Name, subscription.Group.Name);
                continue;
            }

            var checkpoint = await database.Execute(
                new LockCheckpoint(
                    subscriptionId.Value,
                    InitializationConstants.StreamName
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

            var batch = await messageStorage.ReadGlobalStream(
                new ReadGlobalStreamOptions
                {
                    LastGlobalPosition = checkpoint.StreamPosition,
                    BatchSize = subscription.Group.InitializationBatchSize
                },
                cancellationToken
            );

            if (batch.StreamMessages.Count == 0)
            {
                var globalSubscriptionId = registry.GetSubscriptionId(subscription.Group.Name, GlobalCheckpoint.Name);
                if (globalSubscriptionId == null)
                {
                    logger.LogWarning("Global subscription for group {GroupName} not found in mapping", subscription.Group.Name);
                    break;
                }

                var globalCheckpoint = await database.Execute(
                    new LockCheckpoint(
                        globalSubscriptionId.Value,
                        GlobalCheckpoint.StreamName
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                if (globalCheckpoint == null)
                {
                    continue;
                }

                var nextBatch = await messageStorage.ReadGlobalStream(
                    new ReadGlobalStreamOptions
                    {
                        LastGlobalPosition = checkpoint.StreamPosition,
                        BatchSize = 1
                    },
                    cancellationToken
                );

                if (nextBatch.StreamMessages.Any())
                {
                    continue;
                }

                bool setSubscriptionToReplay;

                if (globalCheckpoint.StreamPosition == 0)
                {
                    // subscriptions for a new group can just be activated immediately vs put into replay mode
                    setSubscriptionToReplay = false;
                }
                else if (subscription.Status == SubscriptionStatus.Backfill)
                {
                    // subscriptions being backfilled do not need to reprocess everything
                    setSubscriptionToReplay = false;
                }
                else if (subscription.SkipDuringReplay)
                {
                    // activate the subscription and let the checkpoint processor advance the checkpoints as no-ops
                    setSubscriptionToReplay = false;
                }
                else
                {
                    // if there's no work to catch up on the subscription can be activated immediately
                    var checkpointCount = await database.Execute(
                        new GetSubscriptionCheckpointCount(
                            subscriptionId.Value
                        ),
                        cancellationToken
                    );

                    setSubscriptionToReplay = checkpointCount > 0;
                }

                if (setSubscriptionToReplay)
                {
                    var replaySubscriptionId = registry.GetSubscriptionId(subscription.Group.Name, subscription.Name)!.Value;
                    await database.Execute(
                        new SetSubscriptionToReplay(
                            replaySubscriptionId
                        ),
                        connection,
                        transaction,
                        cancellationToken
                    );
                }
                else
                {
                    if (subscription.Status == SubscriptionStatus.Backfill)
                    {
                        var count = -1;

                        while (count != 0)
                        {
                            var advanceSubscriptionId = registry.GetSubscriptionId(subscription.Group.Name, subscription.Name)!.Value;
                            count = await database.Execute(
                                new AdvanceLaggingSubscriptionCheckpoints(
                                    advanceSubscriptionId
                                ),
                                connection,
                                transaction,
                                cancellationToken
                            );
                        }
                    }

                    await database.Execute(
                        new SetSubscriptionToActive(
                            subscriptionId.Value
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

            foreach (var stream in batch.StreamMessages.GroupBy(x => x.StreamName))
            {
                var filteredStream = stream.Where(x => x.AppliesTo(subscription)).ToArray();

                if (filteredStream.Length == 0)
                {
                    continue;
                }

                var streamVersion = filteredStream.Max(x => x.StreamPosition);

                // if the subscription is set to skip during replay or is being backfilled automatically advance the stream position
                var streamPosition = subscription.SkipDuringReplay || subscription.Status == SubscriptionStatus.Backfill
                    ? streamVersion
                    : 0;

                // track the replay target position
                var globalPosition = filteredStream.Max(x => x.GlobalPosition);

                if (globalPosition > replayTargetPosition.GetValueOrDefault())
                {
                    replayTargetPosition = globalPosition;
                }

                checkpoints.Add(
                    new CheckpointType
                    {
                        SubscriptionId = subscriptionId.Value,
                        StreamName = stream.Key,
                        StreamPosition = streamPosition,
                        StreamVersion = streamVersion
                    }
                );
            }

            await database.Execute(
                new RecordCheckpoints(checkpoints.ToArray()),
                connection,
                transaction,
                cancellationToken
            );

            var newGlobalPosition = batch.StreamMessages.Max(x => x.GlobalPosition);

            await database.Execute(
                new UpdateSystemCheckpointPosition(
                    checkpoint.Id,
                    newGlobalPosition
                ),
                connection,
                transaction,
                cancellationToken
            );

            var updateSubscriptionId = registry.GetSubscriptionId(subscription.Group.Name, subscription.Name)!.Value;
            await database.Execute(
                new UpdateSubscriptionReplayTargetPosition(
                    updateSubscriptionId,
                    replayTargetPosition.GetValueOrDefault()
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
