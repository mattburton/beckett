using System.Threading.Channels;
using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Services;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions;

public class GlobalMessageReader(
    BeckettOptions options,
    Channel<MessagesAvailable> channel,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    ISubscriptionConfigurationCache configurationCache,
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

                    if (!await TryAdvisoryLock(
                            connection,
                            options.Subscriptions.GlobalMessageReaderAdvisoryLockId,
                            stoppingToken
                        ))
                    {
                        logger.CouldNotAcquireGlobalMessageReaderLock();
                        continue;
                    }

                    try
                    {
                        await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                        var currentPosition = await database.Execute(
                            new GetGlobalReaderPosition(),
                            connection,
                            transaction,
                            stoppingToken
                        );

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

                        var checkpoints = await BuildCheckpointsForAllGroups(batch, stoppingToken);
                        var streamMetadata = BuildStreamIndexData(batch);
                        var messageMetadata = BuildMessageIndexData(batch);

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
                                logger.RecordingUpdatedCheckpoints(checkpoints.Count);
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
                    }
                    finally
                    {
                        await AdvisoryUnlock(
                            connection,
                            options.Subscriptions.GlobalMessageReaderAdvisoryLockId,
                            stoppingToken
                        );
                    }

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

    private async Task<HashSet<CheckpointType>> BuildCheckpointsForAllGroups(
        ReadGlobalStreamResult batch,
        CancellationToken cancellationToken
    )
    {
        var checkpoints = new HashSet<CheckpointType>(CheckpointType.Comparer);

        var subscriptions = await configurationCache.GetConfigurations(cancellationToken);

        foreach (var stream in batch.StreamMessages.GroupBy(x => x.StreamName))
        {
            var matchingSubscriptions = subscriptions
                .Where(subscription => SubscriptionAppliesToStream(
                        subscription,
                        stream.Key,
                        stream.Select(m => m.MessageType).ToHashSet()
                    )
                )
                .OrderBy(subscription => subscription.Priority)
                .ToArray();

            foreach (var subscription in matchingSubscriptions)
            {
                logger.NewMessagesFoundForSubscription(
                    subscription.SubscriptionName,
                    subscription.GroupName,
                    stream.Key
                );

                checkpoints.Add(
                    new CheckpointType
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        StreamName = stream.Key,
                        StreamVersion = stream.Max(x => x.StreamPosition)
                    }
                );
            }
        }

        return checkpoints;
    }

    private static bool SubscriptionAppliesToStream(
        SubscriptionConfiguration subscription,
        string streamName,
        HashSet<string> messageTypes
    )
    {
        var isCategoryOnly = subscription is { Category: not null, MessageTypes.Length: 0 };
        var isStreamNameOnly =
            !string.IsNullOrWhiteSpace(subscription.StreamName) && subscription.MessageTypes.Length == 0;
        var isMessageTypesOnly =
            subscription.Category == null && subscription.MessageTypes.Length > 0;
        var isStreamNameAndMessageTypes =
            !string.IsNullOrWhiteSpace(subscription.StreamName) && subscription.MessageTypes.Length > 0;

        if (isCategoryOnly)
        {
            return CategoryMatches(streamName, subscription.Category!);
        }

        if (isStreamNameOnly)
        {
            return subscription.StreamName == streamName;
        }

        if (isMessageTypesOnly)
        {
            return subscription.MessageTypes.Any(messageTypes.Contains);
        }

        if (isStreamNameAndMessageTypes)
        {
            return subscription.StreamName == streamName && subscription.MessageTypes.Any(messageTypes.Contains);
        }

        // Category with message types
        if (subscription is { Category: not null, MessageTypes.Length: > 0 })
        {
            return CategoryMatches(streamName, subscription.Category) &&
                   subscription.MessageTypes.Any(messageTypes.Contains);
        }

        // No valid configuration - subscription doesn't apply
        return false;
    }

    private static bool CategoryMatches(string streamName, string category) => streamName.StartsWith(category);

    private static StreamIndexType[] BuildStreamIndexData(ReadGlobalStreamResult globalStream)
    {
        var streamData = new Dictionary<string, StreamIndexType>();

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
                streamData[message.StreamName] = new StreamIndexType
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

    private static MessageIndexType[] BuildMessageIndexData(ReadGlobalStreamResult globalStream)
    {
        return globalStream.StreamMessages
            .Select(message => new MessageIndexType
                {
                    Id = message.Id,
                    GlobalPosition = message.GlobalPosition,
                    StreamName = message.StreamName,
                    StreamPosition = message.StreamPosition,
                    MessageTypeName = message.MessageType,
                    Category = StreamCategoryParser.Parse(message.StreamName),
                    CorrelationId = message.CorrelationId,
                    Tenant = message.Tenant,
                    Timestamp = message.Timestamp
                }
            )
            .ToArray();
    }

    private static async Task<bool> TryAdvisoryLock(
        NpgsqlConnection connection,
        long advisoryLockId,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select pg_try_advisory_lock({advisoryLockId});";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }

    private static async Task AdvisoryUnlock(
        NpgsqlConnection connection,
        long advisoryLockId,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select pg_advisory_unlock({advisoryLockId});";

        await command.ExecuteScalarAsync(cancellationToken);
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "Starting global message reading")]
    public static partial void StartingGlobalMessageReading(this ILogger logger);

    [LoggerMessage(
        0,
        LogLevel.Debug,
        "Could not acquire advisory lock for global message reader - another instance is already reading"
    )]
    public static partial void CouldNotAcquireGlobalMessageReaderLock(this ILogger logger);

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
