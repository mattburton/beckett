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

    public void Run(CancellationToken cancellationToken)
    {
        if (_task is { IsCompleted: false })
        {
            return;
        }

        _task = Consume(cancellationToken);
    }

    public async Task Consume(CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var checkpoint = await database.Execute(
            new LockCheckpoint(options.ApplicationName, GlobalConstants.GlobalName, GlobalConstants.AllStreamName),
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
                GlobalConstants.GlobalName,
                GlobalConstants.AllStreamName,
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
