using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;

namespace Beckett.Subscriptions;

public class GlobalStreamConsumer(
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    SubscriptionOptions options,
    ISubscriptionRegistry subscriptionRegistry
) : IGlobalStreamConsumer
{
    private Task _task = Task.CompletedTask;
    private bool _pendingRequest;

    public void StartPolling(CancellationToken cancellationToken)
    {
        if (_task is { IsCompleted: false })
        {
            _pendingRequest = true;

            return;
        }

        _task = Poll(cancellationToken);
    }

    private async Task Poll(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(
                    options.ApplicationName,
                    GlobalCheckpoint.Name,
                    GlobalCheckpoint.StreamName
                ),
                connection,
                transaction,
                cancellationToken
            );

            if (checkpoint == null)
            {
                if (!_pendingRequest)
                {
                    break;
                }

                _pendingRequest = false;

                continue;
            }

            var streamChanges = await messageStorage.ReadStreamChanges(
                checkpoint.StreamPosition,
                options.BatchSize,
                cancellationToken
            );

            if (streamChanges.Count == 0)
            {
                if (!_pendingRequest)
                {
                    break;
                }

                _pendingRequest = false;

                continue;
            }

            var checkpoints = new List<CheckpointType>();

            foreach (var streamChange in streamChanges)
            {
                var subscriptions = subscriptionRegistry.All().Where(x => streamChange.AppliesTo(x));

                checkpoints.AddRange(subscriptions.Select(subscription =>
                    new CheckpointType
                    {
                        Application = options.ApplicationName,
                        Name = subscription.Name,
                        StreamName = streamChange.StreamName,
                        StreamVersion = streamChange.StreamVersion
                    }));
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
                    GlobalCheckpoint.Name,
                    GlobalCheckpoint.StreamName,
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
