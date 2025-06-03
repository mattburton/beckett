using System.Threading.Channels;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage;
using Beckett.Subscriptions.PartitionStrategies;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class GlobalStreamConsumer(
    SubscriptionGroup group,
    Channel<MessagesAvailable> channel,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    PostgresOptions postgresOptions,
    ILogger<GlobalStreamConsumer> logger
)
{
    public async Task Poll(CancellationToken stoppingToken)
    {
        var registeredSubscriptions = group.GetSubscriptions().ToArray();

        await foreach (var _ in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.StartingGlobalStreamPolling();

                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var checkpoint = await database.Execute(
                    new LockCheckpoint(
                        group.Name,
                        GlobalCheckpoint.Name,
                        GlobalCheckpoint.StreamName,
                        postgresOptions
                    ),
                    connection,
                    transaction,
                    stoppingToken
                );

                if (checkpoint == null)
                {
                    logger.NoNewGlobalStreamMessagesFound();

                    continue;
                }

                stoppingToken.ThrowIfCancellationRequested();

                var batch = await messageStorage.ReadIndexBatch(
                    new ReadIndexBatchOptions
                    {
                        StartingGlobalPosition = checkpoint.StreamPosition,
                        BatchSize = group.GlobalStreamBatchSize
                    },
                    stoppingToken
                );

                if (batch.Items.Count == 0)
                {
                    logger.NoNewGlobalStreamMessagesFoundAfterPosition(checkpoint.StreamPosition);

                    continue;
                }

                logger.NewGlobalStreamMessagesToProcess(batch.Items.Count, checkpoint.StreamPosition);

                var checkpoints = new HashSet<CheckpointType>(CheckpointType.Comparer);

                var globalStreamSubscriptions = registeredSubscriptions
                    .Where(x => x.PartitionStrategy is GlobalStreamPartitionStrategy)
                    .Where(x => batch.Items.Any(m => m.AppliesTo(x))).OrderBy(x => x.Priority)
                    .ToArray();

                foreach (var subscription in globalStreamSubscriptions)
                {
                    var subscriptionCheckpoint = new CheckpointType
                    {
                        GroupName = group.Name,
                        Name = subscription.Name,
                        StreamName = GlobalStream.Name,
                        StreamVersion = batch.Items.Where(x => x.AppliesTo(subscription))
                            .Max(x => x.GlobalPosition)
                    };

                    if (checkpoints.TryGetValue(subscriptionCheckpoint, out var existingCheckpoint))
                    {
                        existingCheckpoint.StreamVersion = batch.Items
                            .Where(x => x.AppliesTo(subscription)).Max(x => x.GlobalPosition);
                    }
                    else
                    {
                        checkpoints.Add(subscriptionCheckpoint);
                    }
                }

                foreach (var stream in batch.Items.GroupBy(x => x.StreamName))
                {
                    var subscriptions = registeredSubscriptions
                        .Where(x => x.PartitionStrategy is not GlobalStreamPartitionStrategy)
                        .Where(subscription => stream.Any(m => m.AppliesTo(subscription)))
                        .OrderBy(subscription => subscription.Priority).ToArray();

                    foreach (var subscription in subscriptions)
                    {
                        logger.NewMessagesFoundForSubscription(
                            subscription.Name,
                            group.Name,
                            stream.Key
                        );

                        checkpoints.Add(
                            new CheckpointType
                            {
                                GroupName = group.Name,
                                Name = subscription.Name,
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
                        new RecordCheckpoints(checkpoints.ToArray(), postgresOptions),
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

                var newGlobalPosition = batch.Items.Max(x => x.GlobalPosition);

                await database.Execute(
                    new UpdateSystemCheckpointPosition(checkpoint.Id, newGlobalPosition, postgresOptions),
                    connection,
                    transaction,
                    stoppingToken
                );

                RecordStreamData(batch);

                await transaction.CommitAsync(stoppingToken);

                logger.UpdatedGlobalStreamPosition(newGlobalPosition);

                channel.Writer.TryWrite(MessagesAvailable.Instance);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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

    private static void RecordStreamData(ReadIndexBatchResult globalStream)
    {
        var categories = new Dictionary<string, DateTimeOffset>();
        var tenants = new HashSet<string>();

        foreach (var message in globalStream.Items)
        {
            categories[StreamCategoryParser.Parse(message.StreamName)] = message.Timestamp;

            if (string.IsNullOrWhiteSpace(message.Tenant))
            {
                continue;
            }

            tenants.Add(message.Tenant);
        }

        var categoryNames = categories.Keys.ToArray();
        var categoryTimestamps = categories.Values.ToArray();

        StreamDataQueue.Enqueue(categoryNames, categoryTimestamps, tenants.ToArray());
    }
}

public static class StreamCategoryParser
{
    public static string Parse(string streamName)
    {
        return !streamName.Contains('-') ? streamName : streamName[..streamName.IndexOf('-')];
    }
}

public static partial class Log
{
    [LoggerMessage(
        0,
        LogLevel.Trace,
        "New global stream messages are available but consumer is already active - setting continue flag to true"
    )]
    public static partial void NewGlobalStreamMessagesAvailableWhileConsumerIsActive(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "Starting global stream polling")]
    public static partial void StartingGlobalStreamPolling(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "No new messages found for global stream")]
    public static partial void NoNewGlobalStreamMessagesFound(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "No new messages found for global stream after position {Position}")]
    public static partial void NoNewGlobalStreamMessagesFoundAfterPosition(this ILogger logger, long position);

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Found {Count} new global stream message(s) to process after position {Position}"
    )]
    public static partial void NewGlobalStreamMessagesToProcess(this ILogger logger, int count, long position);

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Subscription {SubscriptionName} in group {GroupName} is subscribed to new messages found in stream {StreamName} - updating checkpoint"
    )]
    public static partial void NewMessagesFoundForSubscription(
        this ILogger logger,
        string subscriptionName,
        string groupName,
        string streamName
    );

    [LoggerMessage(0, LogLevel.Trace, "Recording updated stream versions for {Count} checkpoints")]
    public static partial void RecordingUpdatedStreamVersionsForCheckpoints(this ILogger logger, int count);

    [LoggerMessage(0, LogLevel.Trace, "No new checkpoints to record")]
    public static partial void NoNewCheckpointsToRecord(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "Updated global stream to position {Position}")]
    public static partial void UpdatedGlobalStreamPosition(this ILogger logger, long position);
}

public readonly struct MessagesAvailable
{
    public static MessagesAvailable Instance { get; } = new();
}
