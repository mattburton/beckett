using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.Messages.Storage;
using Npgsql;

namespace Beckett.Subscriptions;

public class GlobalStreamConsumer(
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    BeckettOptions options,
    ISubscriptionRegistry subscriptionRegistry,
    IMessageTypeMap messageTypeMap
) : IGlobalStreamConsumer
{
    private const string PostgresLockNotAvailable = "55P03";

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
        var registeredSubscriptions = subscriptionRegistry.All().ToArray();

        var emptyResultRetryCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var checkpoint = await database.Execute(
                    new LockCheckpoint(
                        options.Subscriptions.GroupName,
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

                var globalStream = await messageStorage.ReadGlobalStream(
                    checkpoint.StreamPosition,
                    options.Subscriptions.GlobalBatchSize,
                    stoppingToken
                );

                if (globalStream.Items.Count == 0)
                {
                    emptyResultRetryCount++;

                    if (emptyResultRetryCount <= options.Subscriptions.GlobalEmptyResultsMaxRetryCount)
                    {
                        await transaction.RollbackAsync(stoppingToken);

                        await Task.Delay(options.Subscriptions.GlobalEmptyResultsRetryDelay, stoppingToken);

                        continue;
                    }

                    break;
                }

                var checkpoints = new List<CheckpointType>();

                foreach (var stream in globalStream.Items.GroupBy(x => x.StreamName))
                {
                    var subscriptions = registeredSubscriptions.Where(s => stream.Any(m => m.AppliesTo(s, messageTypeMap)));

                    checkpoints.AddRange(
                        subscriptions.Select(
                            subscription =>
                                new CheckpointType
                                {
                                    GroupName = options.Subscriptions.GroupName,
                                    Name = subscription.Name,
                                    StreamName = stream.Key,
                                    StreamVersion = stream.Max(x => x.StreamPosition)
                                }
                        )
                    );
                }

                if (checkpoints.Count > 0)
                {
                    await database.Execute(
                        new RecordCheckpoints(checkpoints.ToArray()),
                        connection,
                        transaction,
                        stoppingToken
                    );
                }

                var newGlobalPosition = globalStream.Items.Max(x => x.GlobalPosition);

                await database.Execute(
                    new RecordCheckpoint(
                        options.Subscriptions.GroupName,
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
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (NpgsqlException e) when (e.SqlState == PostgresLockNotAvailable)
            {
                //unable to lock checkpoint(s) for update - retry
                if (options.Subscriptions.GlobalLockRetryInterval == TimeSpan.Zero)
                {
                    continue;
                }

                await Task.Delay(options.Subscriptions.GlobalLockRetryInterval, stoppingToken);
            }
        }
    }
}
