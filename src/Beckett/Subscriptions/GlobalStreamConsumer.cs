using Beckett.Database;
using Beckett.Database.Types;
using Beckett.MessageStorage;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class GlobalStreamConsumer(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    BeckettOptions options,
    ILogger<GlobalStreamConsumer> logger
) : IGlobalStreamConsumer
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private Task _task = Task.CompletedTask;
    private bool _continue;

    public void StartPolling(CancellationToken stoppingToken)
    {
        lock (_lock)
        {
            if (_task is { IsCompleted: false })
            {
                logger.NewGlobalStreamMessagesAvailableWhileConsumerIsActive();

                _continue = true;

                return;
            }

            _task = Poll(stoppingToken);
        }
    }

    private async Task Poll(CancellationToken stoppingToken)
    {
        var registeredSubscriptions = SubscriptionRegistry.All().ToArray();

        while (true)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                logger.StartingGlobalStreamPolling();

                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var group = await database.Execute(
                    new LockGroup(
                        options.Subscriptions.GroupId,
                        options.Postgres
                    ),
                    connection,
                    transaction,
                    stoppingToken
                );

                if (group == null)
                {
                    if (_continue)
                    {
                        logger.WillContinuePollingGlobalStream();

                        _continue = false;

                        continue;
                    }

                    logger.NoNewGlobalStreamMessagesFoundExiting();

                    break;
                }

                stoppingToken.ThrowIfCancellationRequested();

                var globalStream = await messageStorage.ReadGlobalStream(
                    group.GlobalPosition,
                    options.Subscriptions.GlobalStreamBatchSize,
                    stoppingToken
                );

                if (globalStream.Items.Count == 0)
                {
                    logger.NoNewGlobalStreamMessagesFoundAfterPosition(group.GlobalPosition);

                    break;
                }

                logger.NewGlobalStreamMessagesToProcess(globalStream.Items.Count, group.GlobalPosition);

                var checkpoints = new List<CheckpointType>();

                foreach (var stream in globalStream.Items.GroupBy(x => x.StreamName))
                {
                    var subscriptions = registeredSubscriptions
                        .Where(subscription => stream.Any(m => m.AppliesTo(subscription)))
                        .OrderBy(subscription => subscription.Priority).ToArray();

                    foreach (var subscription in subscriptions)
                    {
                        logger.NewMessagesFoundForSubscription(subscription.Name, options.Subscriptions.GroupName, stream.Key);

                        checkpoints.Add(
                            new CheckpointType
                            {
                                SubscriptionId = subscription.Id,
                                StreamName = stream.Key,
                                StreamVersion = stream.Max(x => x.StreamPosition)
                            }
                        );
                    }
                }

                stoppingToken.ThrowIfCancellationRequested();

                if (checkpoints.Count > 0)
                {
                    await database.Execute(
                        new RecordCheckpoints(checkpoints.ToArray(), options.Postgres),
                        connection,
                        transaction,
                        stoppingToken
                    );

                    logger.RecordingUpdatedStreamVersionsForCheckpoints(checkpoints.Count);
                }
                else
                {
                    logger.NoNewCheckpointsToRecord();
                }

                var newGlobalPosition = globalStream.Items.Max(x => x.GlobalPosition);

                await database.Execute(
                    new UpdateGroupGlobalPosition(
                        group.Id,
                        newGlobalPosition,
                        options.Postgres
                    ),
                    connection,
                    transaction,
                    stoppingToken
                );

                await transaction.CommitAsync(stoppingToken);

                logger.UpdatedGlobalStreamPosition(newGlobalPosition);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error reading global stream");

                throw;
            }
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "New global stream messages are available but consumer is already active - setting continue flag to true")]
    public static partial void NewGlobalStreamMessagesAvailableWhileConsumerIsActive(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "Starting global stream polling")]
    public static partial void StartingGlobalStreamPolling(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "No new messages found for global stream but will continue polling since the continue flag has been set to true")]
    public static partial void WillContinuePollingGlobalStream(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "No new messages found for global stream - exiting")]
    public static partial void NoNewGlobalStreamMessagesFoundExiting(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "No new messages found for global stream after position {Position} - exiting")]
    public static partial void NoNewGlobalStreamMessagesFoundAfterPosition(this ILogger logger, long position);

    [LoggerMessage(0, LogLevel.Trace, "Found {Count} new global stream message(s) to process after position {Position}")]
    public static partial void NewGlobalStreamMessagesToProcess(this ILogger logger, int count, long position);

    [LoggerMessage(0, LogLevel.Trace, "Subscription {SubscriptionName} in group {GroupName} is subscribed to new messages found in stream {StreamName} - updating checkpoint")]
    public static partial void NewMessagesFoundForSubscription(this ILogger logger, string subscriptionName, string groupName, string streamName);

    [LoggerMessage(0, LogLevel.Trace, "Recording updated stream versions for {Count} checkpoints")]
    public static partial void RecordingUpdatedStreamVersionsForCheckpoints(this ILogger logger, int count);

    [LoggerMessage(0, LogLevel.Trace, "No new checkpoints to record")]
    public static partial void NoNewCheckpointsToRecord(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "Updated global stream to position {Position}")]
    public static partial void UpdatedGlobalStreamPosition(this ILogger logger, long position);
}
