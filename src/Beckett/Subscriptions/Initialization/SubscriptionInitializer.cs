using Beckett.Database;
using Beckett.Database.Types;
using Beckett.MessageStorage;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Initialization;

public class SubscriptionInitializer(
    IPostgresDatabase database,
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
                continue;
            }

            var advisoryLockId = subscription.GetAdvisoryLockId(options.Subscriptions.GroupName);

            var locked = await database.Execute(new TryAdvisoryLock(advisoryLockId), cancellationToken);

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
                await database.Execute(new AdvisoryUnlock(advisoryLockId), cancellationToken);
            }
        }
    }

    private async Task InitializeSubscription(Subscription subscription, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = database.CreateConnection();

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

            logger.LogInformation("Initializing subscription {SubscriptionName}", subscription.Name);

            var globalStream = await messageStorage.ReadGlobalStream(
                checkpoint.StreamPosition,
                options.Subscriptions.InitializationBatchSize,
                cancellationToken
            );

            if (globalStream.Items.Count == 0)
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

                var globalStreamUpdates = await messageStorage.ReadGlobalStream(
                    checkpoint.StreamPosition,
                    1,
                    cancellationToken
                );

                if (globalStreamUpdates.Items.Any())
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

                logger.LogInformation(
                    "Finished initializing subscription {SubscriptionName}",
                    subscription.Name
                );

                break;
            }

            var checkpoints = new List<CheckpointType>();

            foreach (var stream in globalStream.Items.GroupBy(x => x.StreamName))
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

            var newGlobalPosition = globalStream.Items.Max(x => x.GlobalPosition);

            await database.Execute(
                new RecordCheckpoint(
                    options.Subscriptions.GroupName,
                    subscription.Name,
                    InitializationConstants.StreamName,
                    newGlobalPosition,
                    newGlobalPosition,
                    options.Postgres
                ),
                connection,
                transaction,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Initializing subscription {SubscriptionName} - current global position {GlobalPosition}",
                subscription.Name,
                newGlobalPosition
            );
        }
    }
}
