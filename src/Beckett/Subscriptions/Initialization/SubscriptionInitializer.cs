using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Events;

namespace Beckett.Subscriptions.Initialization;

public class SubscriptionInitializer(
    IPostgresDatabase database,
    SubscriptionOptions options,
    IEventStorage eventStorage,
    ISubscriptionRegistry subscriptionRegistry,
    IEventTypeMap eventTypeMap
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
            var subscriptionName = await database.Execute(new GetNextUninitializedSubscription(), cancellationToken);

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
                new LockCheckpoint(subscription.Name, InitializationConstants.StreamName),
                connection,
                transaction,
                cancellationToken
            );

            if (checkpoint == null)
            {
                break;
            }

            var streamChanges = await eventStorage.ReadStreamChanges(
                checkpoint.StreamPosition,
                options.BatchSize,
                cancellationToken
            );

            if (streamChanges.Count == 0)
            {
                await database.Execute(
                    new LockCheckpoint(GlobalConstants.GlobalName, GlobalConstants.AllStreamName),
                    connection,
                    transaction,
                    cancellationToken
                );

                var newStreamChanges = await eventStorage.ReadStreamChanges(
                    checkpoint.StreamPosition,
                    options.BatchSize,
                    cancellationToken
                );

                if (newStreamChanges.Any())
                {
                    continue;
                }

                await database.Execute(
                    new SetSubscriptionToInitialized(subscription.Name),
                    connection,
                    transaction,
                    cancellationToken
                );

                await transaction.CommitAsync(cancellationToken);

                break;
            }

            var subscriptionEventTypes = subscription.EventTypes.Select(eventTypeMap.GetName).ToArray();

            var checkpoints = new List<CheckpointType>();

            foreach (var streamChange in streamChanges)
            {
                if (!subscriptionEventTypes.Intersect(streamChange.EventTypes).Any())
                {
                    continue;
                }

                checkpoints.Add(new CheckpointType
                {
                    Name = subscription.Name,
                    StreamName = streamChange.StreamName,
                    StreamVersion = streamChange.StreamVersion
                });
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
