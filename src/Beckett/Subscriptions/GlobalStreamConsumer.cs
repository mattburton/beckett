using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.Messages.Storage;

namespace Beckett.Subscriptions;

public class GlobalStreamConsumer(
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    BeckettOptions options,
    ISubscriptionRegistry subscriptionRegistry,
    IMessageTypeMap messageTypeMap
) : IGlobalStreamConsumer
{
    private Task _task = Task.CompletedTask;

    public void StartPolling(CancellationToken stoppingToken)
    {
        if (_task is { IsCompleted: false })
        {
            return;
        }

        _task = Poll(stoppingToken);
    }

    private async Task Poll(CancellationToken stoppingToken)
    {
        var emptyResultRetryCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(stoppingToken);

            await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(
                    options.ApplicationName,
                    GlobalCheckpoint.Name,
                    GlobalCheckpoint.StreamName
                ),
                connection,
                transaction,
                stoppingToken
            );

            if (checkpoint == null)
            {
                break;
            }

            var streamChanges = await messageStorage.ReadStreamChangeFeed(
                checkpoint.StreamPosition,
                options.Subscriptions.GlobalBatchSize,
                stoppingToken
            );

            if (streamChanges.Items.Count == 0)
            {
                emptyResultRetryCount++;

                if (emptyResultRetryCount <= options.Subscriptions.GlobalEmptyResultsMaxRetryCount)
                {
                    await Task.Delay(options.Subscriptions.GlobalEmptyResultsRetryDelay, stoppingToken);

                    continue;
                }

                break;
            }

            var checkpoints = new List<CheckpointType>();

            foreach (var streamChange in streamChanges.Items)
            {
                var subscriptions = subscriptionRegistry.All().Where(x => streamChange.AppliesTo(x, messageTypeMap));

                checkpoints.AddRange(
                    subscriptions.Select(
                        subscription =>
                            new CheckpointType
                            {
                                Application = options.ApplicationName,
                                Name = subscription.Name,
                                StreamName = streamChange.StreamName,
                                StreamVersion = streamChange.StreamVersion
                            }
                    )
                );
            }

            await database.Execute(
                new RecordCheckpoints(checkpoints.ToArray()),
                connection,
                transaction,
                stoppingToken
            );

            var newGlobalPosition = streamChanges.Items.Max(x => x.GlobalPosition);

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
                stoppingToken
            );

            await transaction.CommitAsync(stoppingToken);
        }
    }
}
