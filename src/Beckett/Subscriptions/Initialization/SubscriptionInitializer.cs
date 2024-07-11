using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.Messages.Storage;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Initialization;

public class SubscriptionInitializer(
    IPostgresDatabase database,
    BeckettOptions options,
    IMessageStorage messageStorage,
    ISubscriptionRegistry subscriptionRegistry,
    IMessageTypeMap messageTypeMap,
    ILogger<SubscriptionInitializer> logger
) : ISubscriptionInitializer
{
    private Task _task = Task.CompletedTask;

    public void Start(CancellationToken cancellationToken)
    {
        if (_task is { IsCompleted: false })
        {
            return;
        }

        _task = InitializeSubscriptions(cancellationToken);
    }

    private async Task InitializeSubscriptions(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var subscriptionName = await database.Execute(
                new GetNextUninitializedSubscription(options.ApplicationName),
                cancellationToken
            );

            if (subscriptionName == null)
            {
                break;
            }

            var subscription = subscriptionRegistry.GetSubscription(subscriptionName);

            if (subscription == null)
            {
                continue;
            }

            var advisoryLockId = subscription.GetAdvisoryLockId(options.ApplicationName);

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
        logger.LogInformation("Initializing subscription {SubscriptionName}", subscription.Name);

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(
                    options.ApplicationName,
                    subscription.Name,
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

            var globalStream = await messageStorage.ReadGlobalStream(
                checkpoint.StreamPosition,
                options.Subscriptions.BatchSize,
                cancellationToken
            );

            if (globalStream.Items.Count == 0)
            {
                await database.Execute(
                    new LockCheckpoint(
                        options.ApplicationName,
                        GlobalCheckpoint.Name,
                        GlobalCheckpoint.StreamName
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                var globalStreamUpdates = await messageStorage.ReadGlobalStream(
                    checkpoint.StreamPosition,
                    options.Subscriptions.BatchSize,
                    cancellationToken
                );

                if (globalStreamUpdates.Items.Any())
                {
                    continue;
                }

                await database.Execute(
                    new SetSubscriptionToInitialized(options.ApplicationName, subscription.Name),
                    connection,
                    transaction,
                    cancellationToken
                );

                await transaction.CommitAsync(cancellationToken);

                break;
            }

            var checkpoints = new List<CheckpointType>();

            foreach (var stream in globalStream.Items.GroupBy(x => x.StreamName))
            {
                if (stream.All(x => !x.AppliesTo(subscription, messageTypeMap)))
                {
                    continue;
                }

                checkpoints.Add(
                    new CheckpointType
                    {
                        Application = options.ApplicationName,
                        Name = subscription.Name,
                        StreamName = stream.Key,
                        StreamVersion = stream.Max(x => x.StreamPosition)
                    }
                );
            }

            await database.Execute(
                new RecordCheckpoints(checkpoints.ToArray()),
                connection,
                transaction,
                cancellationToken
            );

            var newGlobalPosition = globalStream.Items.Max(x => x.GlobalPosition);

            await database.Execute(
                new RecordCheckpoint(
                    options.ApplicationName,
                    subscription.Name,
                    InitializationConstants.StreamName,
                    newGlobalPosition,
                    newGlobalPosition
                ),
                connection,
                transaction,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
