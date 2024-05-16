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

    public void Consume(CancellationToken cancellationToken)
    {
        if (_task is { IsCompleted: false })
        {
            return;
        }

        _task = Execute(cancellationToken);
    }

    private async Task Execute(CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var checkpoint = await database.Execute(
            new LockCheckpoint(
                options.ApplicationName,
                GlobalCheckpoint.Name,
                GlobalCheckpoint.Topic,
                GlobalCheckpoint.StreamId
            ),
            connection,
            transaction,
            cancellationToken
        );

        if (checkpoint == null)
        {
            return;
        }

        var streamChanges = await messageStorage.ReadStreamChanges(
            checkpoint.StreamPosition,
            options.BatchSize,
            cancellationToken
        );

        if (streamChanges.Count == 0)
        {
            return;
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
                    Topic = streamChange.Topic,
                    StreamId = streamChange.StreamId,
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
                GlobalCheckpoint.Topic,
                GlobalCheckpoint.StreamId,
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
