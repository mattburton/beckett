using System.Threading.Channels;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class GlobalMessageReader(
    BeckettOptions options,
    Channel<MessagesAvailable> channel,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    ISubscriptionRegistry registry,
    ILogger<GlobalMessageReader> logger
)
{
    public async Task Poll(CancellationToken stoppingToken)
    {
        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channel.Reader.TryRead(out _))
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    logger.StartingGlobalMessageReading();

                    await using var connection = dataSource.CreateConnection();
                    await connection.OpenAsync(stoppingToken);
                    await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                    // Get current global reader position
                    var currentPosition = await database.Execute(
                        new GetGlobalReaderPosition(),
                        connection,
                        transaction,
                        stoppingToken
                    );

                    // Read batch from global stream
                    var batch = await messageStorage.ReadGlobalStream(
                        new ReadGlobalStreamOptions
                        {
                            LastGlobalPosition = currentPosition,
                            BatchSize = options.Subscriptions.GlobalStreamBatchSize
                        },
                        stoppingToken
                    );

                    if (batch.StreamMessages.Count == 0)
                    {
                        logger.NoNewGlobalMessagesFound(currentPosition);
                        continue;
                    }

                    logger.NewGlobalMessagesToProcess(batch.StreamMessages.Count, currentPosition);

                    // Build checkpoints for all subscription groups
                    var allCheckpoints = BuildCheckpointsForAllGroups(batch);

                    // Build metadata
                    var streamMetadata = BuildStreamMetadata(batch);
                    var messageMetadata = BuildMessageMetadata(batch);

                    if (allCheckpoints.Count > 0 || streamMetadata.Length > 0)
                    {
                        await database.Execute(
                            new RecordCheckpointsAndMetadata(
                                allCheckpoints.ToArray(),
                                streamMetadata,
                                messageMetadata
                            ),
                            connection,
                            transaction,
                            stoppingToken
                        );

                        if (allCheckpoints.Count > 0)
                        {
                            logger.RecordingUpdatedCheckpoints(allCheckpoints.Count);
                        }
                    }

                    // Update global reader position
                    var newGlobalPosition = batch.StreamMessages.Max(x => x.GlobalPosition);
                    await database.Execute(
                        new UpdateGlobalReaderPosition(newGlobalPosition),
                        connection,
                        transaction,
                        stoppingToken
                    );

                    await transaction.CommitAsync(stoppingToken);

                    logger.UpdatedGlobalReaderPosition(newGlobalPosition);

                    // Continue reading
                    channel.Writer.TryWrite(MessagesAvailable.Instance);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error reading global message stream");
                    throw;
                }
            }
        }
    }

    private HashSet<CheckpointType> BuildCheckpointsForAllGroups(ReadGlobalStreamResult batch)
    {
        var checkpoints = new HashSet<CheckpointType>(CheckpointType.Comparer);

        // Process each subscription group
        foreach (var group in options.Subscriptions.Groups)
        {
            var registeredSubscriptions = group.GetSubscriptions().ToArray();

            foreach (var stream in batch.StreamMessages.GroupBy(x => x.StreamName))
            {
                var subscriptions = registeredSubscriptions
                    .Where(subscription => stream.Any(m => m.AppliesTo(subscription)))
                    .OrderBy(subscription => subscription.Priority)
                    .ToArray();

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
        }

        return checkpoints;
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

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "Starting global message reading")]
    public static partial void StartingGlobalMessageReading(this ILogger logger);

    [LoggerMessage(0, LogLevel.Trace, "No new messages found for global reader after position {Position}")]
    public static partial void NoNewGlobalMessagesFound(this ILogger logger, long position);

    [LoggerMessage(0, LogLevel.Trace, "Found {Count} new global message(s) to process after position {Position}")]
    public static partial void NewGlobalMessagesToProcess(this ILogger logger, int count, long position);

    [LoggerMessage(0, LogLevel.Trace, "Recording {Count} updated checkpoints")]
    public static partial void RecordingUpdatedCheckpoints(this ILogger logger, int count);

    [LoggerMessage(0, LogLevel.Trace, "Updated global reader position to {Position}")]
    public static partial void UpdatedGlobalReaderPosition(this ILogger logger, long position);

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
}