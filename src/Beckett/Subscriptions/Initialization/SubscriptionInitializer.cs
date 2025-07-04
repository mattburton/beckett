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
        long? replayTargetPosition = null;

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

            var batch = await messageStorage.ReadGlobalStream(
                new ReadGlobalStreamOptions
                {
                    LastGlobalPosition = checkpoint.StreamPosition,
                    BatchSize = options.Subscriptions.InitializationBatchSize
                },
                cancellationToken
            );

            if (batch.StreamMessages.Count == 0)
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
                            subscription.Group.Name,
                            subscription.Name,
                            options.Postgres
                        ),
                        cancellationToken
                    );

                    setSubscriptionToReplay = checkpointCount > 0;
                }

                if (setSubscriptionToReplay)
                {
                    await database.Execute(
                        new SetSubscriptionToReplay(
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
                    if (subscription.Status == SubscriptionStatus.Backfill)
                    {
                        var count = -1;

                        while (count != 0)
                        {
                            count = await database.Execute(
                                new AdvanceLaggingSubscriptionCheckpoints(
                                    subscription.Group.Name,
                                    subscription.Name,
                                    options.Postgres
                                ),
                                connection,
                                transaction,
                                cancellationToken
                            );
                        }
                    }

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

                await transaction.CommitAsync(cancellationToken);

                logger.FinishedInitializingSubscription(subscription.Name);

                break;
            }

            var checkpoints = new List<CheckpointType>();

            foreach (var stream in batch.StreamMessages.GroupBy(x => x.StreamName))
            {
                if (stream.All(x => !x.AppliesTo(subscription)))
                {
                    continue;
                }

                var streamVersion = stream.Max(x => x.StreamPosition);

                // if the subscription is set to skip during replay or is being backfilled automatically advance the stream position
                var streamPosition = subscription.SkipDuringReplay || subscription.Status == SubscriptionStatus.Backfill
                    ? streamVersion
                    : 0;

                // track the replay target position
                var globalPosition = stream.Max(x => x.GlobalPosition);

                if (globalPosition > replayTargetPosition.GetValueOrDefault())
                {
                    replayTargetPosition = globalPosition;
                }

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

            var newGlobalPosition = batch.StreamMessages.Max(x => x.GlobalPosition);

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

            await database.Execute(
                new UpdateSubscriptionReplayTargetPosition(
                    subscription.Group.Name,
                    subscription.Name,
                    replayTargetPosition.GetValueOrDefault(),
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
