using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;

namespace Beckett.Subscriptions.Initialization;

public class SubscriptionInitializer(
    IPostgresDatabase database,
    BeckettOptions options,
    IMessageStorage messageStorage,
    ISubscriptionRegistry subscriptionRegistry,
    IMessageTypeMap messageTypeMap
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

            var advisoryLockId = subscription.Name.GetDeterministicHashCode();

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

            var streamChanges = await messageStorage.ReadStreamChangeFeed(
                checkpoint.StreamPosition,
                options.Subscriptions.BatchSize,
                cancellationToken
            );

            if (streamChanges.Count == 0)
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

                var newStreamChanges = await messageStorage.ReadStreamChangeFeed(
                    checkpoint.StreamPosition,
                    options.Subscriptions.BatchSize,
                    cancellationToken
                );

                if (newStreamChanges.Any())
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

            var subscriptionMessageTypes = subscription.MessageTypes.Select(messageTypeMap.GetName).ToArray();

            var checkpoints = new List<CheckpointType>();

            foreach (var streamChange in streamChanges)
            {
                if (!subscriptionMessageTypes.Intersect(streamChange.MessageTypes).Any())
                {
                    continue;
                }

                checkpoints.Add(
                    new CheckpointType
                    {
                        Application = options.ApplicationName,
                        Name = subscription.Name,
                        StreamName = streamChange.StreamName,
                        StreamVersion = streamChange.StreamVersion
                    }
                );
            }

            await database.Execute(
                new RecordCheckpoints(checkpoints.ToArray()),
                connection,
                transaction,
                cancellationToken
            );

            var newGlobalPosition = streamChanges.Max(x => x.GlobalPosition);

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
