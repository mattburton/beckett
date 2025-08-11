using System.Threading.Channels;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Services;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class GlobalStreamConsumer(
    SubscriptionGroup group,
    Channel<MessagesAvailable> channel,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    ISubscriptionRegistry registry,
    ILogger<GlobalStreamConsumer> logger
)
{
    public async Task Poll(CancellationToken stoppingToken)
    {
        var registeredSubscriptions = group.GetSubscriptions().ToArray();

        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channel.Reader.TryRead(out _))
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    logger.StartingGlobalStreamPolling();

                    await using var connection = dataSource.CreateConnection();

                    await connection.OpenAsync(stoppingToken);

                    await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                    var globalSubscriptionId = registry.GetSubscriptionId(group.Name, GlobalCheckpoint.Name);
                    if (globalSubscriptionId == null)
                    {
                        logger.LogWarning("Global subscription not found for group {GroupName}", group.Name);
                        continue;
                    }

                    var checkpoint = await database.Execute(
                        new LockCheckpoint(
                            globalSubscriptionId.Value,
                            GlobalCheckpoint.StreamName
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

                    var batch = await messageStorage.ReadGlobalStream(
                        new ReadGlobalStreamOptions
                        {
                            LastGlobalPosition = checkpoint.StreamPosition,
                            BatchSize = group.GlobalStreamBatchSize
                        },
                        stoppingToken
                    );

                    if (batch.StreamMessages.Count == 0)
                    {
                        logger.NoNewGlobalStreamMessagesFoundAfterPosition(checkpoint.StreamPosition);

                        continue;
                    }

                    logger.NewGlobalStreamMessagesToProcess(batch.StreamMessages.Count, checkpoint.StreamPosition);

                    var checkpoints = new HashSet<CheckpointType>(CheckpointType.Comparer);

                    foreach (var stream in batch.StreamMessages.GroupBy(x => x.StreamName))
                    {
                        var subscriptions = registeredSubscriptions
                            .Where(subscription => stream.Any(m => m.AppliesTo(subscription)))
                            .OrderBy(subscription => subscription.Priority).ToArray();

                        foreach (var subscription in subscriptions)
                        {
                            logger.NewMessagesFoundForSubscription(
                                subscription.Name,
                                group.Name,
                                stream.Key
                            );

                            var subscriptionId = registry.GetSubscriptionId(group.Name, subscription.Name);
                            if (subscriptionId.HasValue)
                            {
                                checkpoints.Add(
                                    new CheckpointType
                                    {
                                        SubscriptionId = subscriptionId.Value,
                                        StreamName = stream.Key,
                                        StreamVersion = stream.Max(x => x.StreamPosition)
                                    }
                                );
                            }
                        }
                    }

                    stoppingToken.ThrowIfCancellationRequested();

                    // Build metadata from the batch
                    var streamMetadata = BuildStreamMetadata(batch);
                    var messageMetadata = BuildMessageMetadata(batch);

                    if (checkpoints.Count > 0 || streamMetadata.Length > 0)
                    {
                        await database.Execute(
                            new RecordCheckpointsAndMetadata(
                                checkpoints.ToArray(),
                                streamMetadata,
                                messageMetadata
                            ),
                            connection,
                            transaction,
                            stoppingToken
                        );

                        if (checkpoints.Count > 0)
                        {
                            logger.RecordingUpdatedStreamVersionsForCheckpoints(checkpoints.Count);
                        }
                    }
                    else
                    {
                        logger.NoNewCheckpointsToRecord();
                    }

                    var newGlobalPosition = batch.StreamMessages.Max(x => x.GlobalPosition);

                    await database.Execute(
                        new UpdateSystemCheckpointPosition(checkpoint.Id, newGlobalPosition),
                        connection,
                        transaction,
                        stoppingToken
                    );

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
    }

    private static StreamMetadataType[] BuildStreamMetadata(ReadGlobalStreamResult globalStream)
    {
        var streamData = new Dictionary<string, StreamMetadataType>();

        foreach (var message in globalStream.StreamMessages)
        {
            var category = StreamCategoryParser.Parse(message.StreamName);
            
            if (streamData.TryGetValue(message.StreamName, out var existing))
            {
                existing.LatestPosition = Math.Max(existing.LatestPosition, message.StreamPosition);
                existing.LatestGlobalPosition = Math.Max(existing.LatestGlobalPosition, message.GlobalPosition);
                existing.MessageCount++;
            }
            else
            {
                streamData[message.StreamName] = new StreamMetadataType
                {
                    StreamName = message.StreamName,
                    Category = category,
                    LatestPosition = message.StreamPosition,
                    LatestGlobalPosition = message.GlobalPosition,
                    MessageCount = 1
                };
            }
        }

        return streamData.Values.ToArray();
    }

    private static MessageMetadataType[] BuildMessageMetadata(ReadGlobalStreamResult globalStream)
    {
        return globalStream.StreamMessages
            .Select(message => new MessageMetadataType
            {
                Id = message.Id,
                GlobalPosition = message.GlobalPosition,
                StreamName = message.StreamName,
                StreamPosition = message.StreamPosition,
                Type = message.MessageType,
                Category = StreamCategoryParser.Parse(message.StreamName),
                CorrelationId = message.CorrelationId,
                Tenant = message.Tenant,
                Timestamp = message.Timestamp
            })
            .ToArray();
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
