using Beckett.Database;
using Beckett.OpenTelemetry;
using Beckett.Storage;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointProcessor(
    IMessageStorage messageStorage,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IServiceProvider serviceProvider,
    IInstrumentation instrumentation,
    ILogger<CheckpointProcessor> logger
) : ICheckpointProcessor
{
    public async Task Process(int instance, Checkpoint checkpoint, Subscription subscription)
    {
        using var checkpointLoggingScope = CheckpointLoggingScope(instance, checkpoint);

        logger.ProcessingCheckpoint(checkpoint.Id, checkpoint.StreamPosition, checkpoint.StreamVersion);

        var result = await HandleMessageBatch(checkpoint, subscription);

        switch (result)
        {
            case Success success:
            {
                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(CancellationToken.None);

                await using var transaction = await connection.BeginTransactionAsync(CancellationToken.None);

                await database.Execute(
                    new UpdateCheckpointPosition(checkpoint.Id, success.StreamPosition, null),
                    connection,
                    transaction,
                    CancellationToken.None
                );

                if (checkpoint.ReplayTargetPosition.HasValue)
                {
                    if (success.GlobalPosition >= checkpoint.ReplayTargetPosition.Value)
                    {
                        await database.Execute(
                            new SetSubscriptionToActive(subscription.Group.Name, subscription.Name),
                            connection,
                            transaction,
                            CancellationToken.None
                        );
                    }
                }

                await transaction.CommitAsync(CancellationToken.None);

                SuccessTraceLogging(checkpoint, success);

                break;
            }
            case Error error:
                var exceptionType = error.Exception.GetType();

                var maxRetryCount = subscription.GetMaxRetryCount(exceptionType);

                var attempt = checkpoint.IsRetryOrFailure ? checkpoint.RetryAttempts : 0;

                var status = attempt >= maxRetryCount ? CheckpointStatus.Failed : CheckpointStatus.Retry;

                DateTimeOffset? processAt = status == CheckpointStatus.Retry
                    ? attempt.GetNextDelayWithExponentialBackoff(
                        subscription.Group.Retries.InitialDelay,
                        subscription.Group.Retries.MaxDelay
                    )
                    : null;

                ErrorTraceLogging(checkpoint, status, error, processAt, exceptionType, attempt, maxRetryCount);

                await database.Execute(
                    new RecordCheckpointError(
                        checkpoint.Id,
                        error.StreamPosition,
                        status,
                        attempt,
                        ExceptionData.From(error.Exception).ToJson(),
                        processAt
                    ),
                    CancellationToken.None
                );

                break;
        }
    }

    private async Task<IMessageBatchResult> HandleMessageBatch(Checkpoint checkpoint, Subscription subscription)
    {
        ReadMessageBatchResult batch;

        try
        {
            batch = await ReadMessageBatch(checkpoint, subscription);
        }
        catch (Exception e)
        {
            var streamPosition = checkpoint.StreamPosition + 1;

            logger.LogError(
                e,
                "Error reading message batch for subscription {SubscriptionType} for stream {StreamName} at {StreamPosition} [Checkpoint: {CheckpointId}]",
                subscription.Name,
                checkpoint.StreamName,
                streamPosition,
                checkpoint.Id
            );

            return new Error(streamPosition, e);
        }

        try
        {
            if (batch.Messages.Count == 0)
            {
                if (batch.StreamVersion == 0 || checkpoint.StreamPosition >= checkpoint.StreamVersion)
                {
                    return NoMessages.Instance;
                }

                // when processing batches within streams we could have a scenario where we read the next batch of
                // messages but then after filters were applied no actual messages were returned. in that case we need
                // to advance the checkpoint position, so we do that by returning the lesser value of either the next
                // position based on batch size or the stream version
                var nextPositionBasedOnBatchSize = checkpoint.StreamPosition + subscription.Group.SubscriptionBatchSize;

                var nextPosition = Math.Min(nextPositionBasedOnBatchSize, checkpoint.StreamVersion);

                return new Success(nextPosition, batch.EndingGlobalPosition);
            }

            var subscriptionContext = BuildSubscriptionContext(checkpoint, subscription);

            if (subscription.SkipDuringReplay && subscriptionContext.IsReplay)
            {
                return new Success(batch.EndingStreamPosition, batch.EndingGlobalPosition);
            }

            if (subscription.Handler.IsBatchHandler)
            {
                try
                {
                    return await DispatchMessageBatchToHandler(
                        checkpoint,
                        subscription,
                        batch,
                        subscriptionContext
                    );
                }
                catch (BatchHandlerException e)
                {
                    LogDispatchError(checkpoint, subscription, e);

                    return new Error(e.StreamPosition, e.InnerException ?? e);
                }
                catch (Exception e)
                {
                    LogDispatchError(checkpoint, subscription, e);

                    return new Error(batch.Messages[0].StreamPosition, e);
                }
            }

            foreach (var streamMessage in batch.Messages.Where(x => subscription.SubscribedToMessage(x.Type)))
            {
                try
                {
                    await DispatchMessageToHandler(checkpoint, subscription, streamMessage, subscriptionContext);
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching message {MessageType} to subscription {SubscriptionType} for stream {StreamName} [Message ID: {MessageId}, Checkpoint: {CheckpointId}]",
                        streamMessage.Type,
                        subscription.Name,
                        checkpoint.StreamName,
                        streamMessage.Id,
                        checkpoint.Id
                    );

                    return new Error(streamMessage.StreamPosition, e);
                }
            }

            return new Success(batch.EndingStreamPosition, batch.EndingGlobalPosition);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception processing checkpoint {CheckpointId}", checkpoint.Id);

            throw;
        }
    }

    private async Task DispatchMessageToHandler(
        Checkpoint checkpoint,
        Subscription subscription,
        IMessageContext messageContext,
        ISubscriptionContext subscriptionContext
    )
    {
        using var activity = instrumentation.StartHandleMessageActivity(subscription, messageContext);

        using var messageLoggingScope = MessageLoggingScope(messageContext);

        logger.HandlingMessageForCheckpoint(messageContext.Id, checkpoint.Id);

        using var scope = serviceProvider.CreateScope();

        using var cts = new CancellationTokenSource();

        cts.CancelAfter(subscription.Group.ReservationTimeout);

        try
        {
            await subscription.Handler.Invoke(
                messageContext,
                subscriptionContext,
                scope.ServiceProvider,
                logger,
                cts.Token
            );
        }
        //special handling for HttpClient timeouts - see https://github.com/dotnet/runtime/issues/21965
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException timeoutException)
        {
            throw timeoutException;
        }
        catch (OperationCanceledException e) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Handler exceeded the reservation timeout of {subscription.Group.ReservationTimeout.TotalSeconds} seconds while handling message {messageContext.Id} for checkpoint {checkpoint.Id}",
                e
            );
        }
    }

    private async Task<IMessageBatchResult> DispatchMessageBatchToHandler(
        Checkpoint checkpoint,
        Subscription subscription,
        ReadMessageBatchResult batch,
        ISubscriptionContext subscriptionContext
    )
    {
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(subscription.Group.ReservationTimeout);

        try
        {
            using var scope = serviceProvider.CreateScope();

            await subscription.Handler.Invoke(
                batch.Messages,
                subscriptionContext,
                scope.ServiceProvider,
                logger,
                cts.Token
            );

            return new Success(batch.EndingStreamPosition, batch.EndingGlobalPosition);
        }
        //special handling for HttpClient timeouts - see https://github.com/dotnet/runtime/issues/21965
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException timeoutException)
        {
            throw timeoutException;
        }
        catch (OperationCanceledException e) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Handler exceeded the reservation timeout of {subscription.Group.ReservationTimeout.TotalSeconds} seconds while handling message batch starting with {batch.Messages[0].Id} for checkpoint {checkpoint.Id}",
                e
            );
        }
    }

    private async Task<ReadMessageBatchResult> ReadMessageBatch(Checkpoint checkpoint, Subscription subscription)
    {
        var startingStreamPosition = checkpoint.StreamPosition + 1;
        var endingStreamPosition = checkpoint.StreamVersion;
        var batchSize = subscription.BatchSize ?? subscription.Group.SubscriptionBatchSize;

        if (checkpoint.IsRetryOrFailure)
        {
            startingStreamPosition = checkpoint.StreamPosition;

            endingStreamPosition = startingStreamPosition + 1;
        }
        else
        {
            var count = endingStreamPosition - checkpoint.StreamPosition;

            if (count > batchSize)
            {
                endingStreamPosition = checkpoint.StreamPosition + batchSize;
            }
        }

        var stream = await messageStorage.ReadStream(
            checkpoint.StreamName,
            new ReadStreamOptions
            {
                StartingStreamPosition = startingStreamPosition,
                EndingStreamPosition = endingStreamPosition,
                Types = subscription.MessageTypeNames.ToArray()
            },
            CancellationToken.None
        );

        logger.FoundMessagesToProcessForCheckpoint(stream.StreamMessages.Count, checkpoint.Id);

        if (stream.StreamMessages.Count <= 0)
        {
            return new ReadMessageBatchResult(
                stream.StreamMessages.Select(MessageContext.From).ToList(),
                stream.StreamVersion,
                0,
                0
            );
        }

        var lastStreamMessage = stream.StreamMessages[^1];

        return new ReadMessageBatchResult(
            stream.StreamMessages.Select(MessageContext.From).ToList(),
            stream.StreamVersion,
            lastStreamMessage.StreamPosition,
            lastStreamMessage.GlobalPosition
        );
    }

    private void LogDispatchError(Checkpoint checkpoint, Subscription subscription, Exception e) =>
        logger.LogError(
            e,
            "Error dispatching message batch to subscription {SubscriptionName} for stream {StreamName} [Checkpoint: {CheckpointId}]",
            subscription.Name,
            checkpoint.StreamName,
            checkpoint.Id
        );

    private static SubscriptionContext BuildSubscriptionContext(Checkpoint checkpoint, Subscription subscription) =>
        new(
            subscription.Group.Name,
            subscription.Name,
            checkpoint.ReplayTargetPosition.HasValue ? SubscriptionStatus.Replay : SubscriptionStatus.Active
        );

    private void SuccessTraceLogging(Checkpoint checkpoint, Success success)
    {
        if (checkpoint.Status == CheckpointStatus.Active)
        {
            logger.CheckpointProcessedSuccessfully(checkpoint.Id, success.StreamPosition);
        }
        else
        {
            logger.RetryAttemptSucceeded(checkpoint.RetryAttempts + 1, checkpoint.Id, success.StreamPosition);
        }
    }

    private void ErrorTraceLogging(
        Checkpoint checkpoint,
        CheckpointStatus status,
        Error error,
        DateTimeOffset? processAt,
        Type exceptionType,
        int attempt,
        int maxRetryCount
    )
    {
        if (checkpoint.Status == CheckpointStatus.Active)
        {
            if (status == CheckpointStatus.Retry)
            {
                logger.CheckpointWillStartRetry(checkpoint.Id, error.StreamPosition, processAt);
            }
            else
            {
                logger.CheckpointWillMoveToFailedImmediately(checkpoint.Id, error.StreamPosition, exceptionType);
            }

            return;
        }

        if (status == CheckpointStatus.Retry)
        {
            logger.RetryAttemptFailedWillTryAgain(
                checkpoint.Id,
                error.StreamPosition,
                attempt,
                maxRetryCount,
                processAt
            );
        }
        else
        {
            logger.RetryAttemptFailedMovingCheckpointToFailed(
                checkpoint.Id,
                error.StreamPosition,
                attempt,
                maxRetryCount
            );
        }
    }

    private IDisposable? CheckpointLoggingScope(int instance, Checkpoint checkpoint)
    {
        const string id = "beckett.checkpoint.id";
        const string groupName = "beckett.checkpoint.group_name";
        const string name = "beckett.checkpoint.name";
        const string streamName = "beckett.checkpoint.stream_name";
        const string streamPosition = "beckett.checkpoint.stream_position";
        const string streamVersion = "beckett.checkpoint.stream_version";
        const string consumer = "beckett.checkpoint.consumer";

        if (!logger.IsEnabled(LogLevel.Information))
        {
            return NoOpDisposable.Instance;
        }

        var state = new Dictionary<string, object>
        {
            { id, checkpoint.Id }
        };

        if (!logger.IsEnabled(LogLevel.Trace))
        {
            return logger.BeginScope(state);
        }

        state.Add(groupName, checkpoint.GroupName);
        state.Add(name, checkpoint.Name);
        state.Add(streamName, checkpoint.StreamName);
        state.Add(streamPosition, checkpoint.StreamPosition);
        state.Add(streamVersion, checkpoint.StreamVersion);
        state.Add(consumer, instance);

        return logger.BeginScope(state);
    }

    private IDisposable? MessageLoggingScope(IMessageContext messageContext)
    {
        const string id = "beckett.message.id";
        const string type = "beckett.message.type";
        const string globalPosition = "beckett.message.global_position";
        const string streamPosition = "beckett.message.stream_position";

        if (!logger.IsEnabled(LogLevel.Information))
        {
            return NoOpDisposable.Instance;
        }

        var state = new Dictionary<string, object>
        {
            { id, messageContext.Id }
        };

        if (!logger.IsEnabled(LogLevel.Trace))
        {
            return logger.BeginScope(state);
        }

        state.Add(type, messageContext.Type);
        state.Add(globalPosition, messageContext.GlobalPosition);
        state.Add(streamPosition, messageContext.StreamPosition);

        return logger.BeginScope(state);
    }

    private interface IMessageBatchResult;

    private record ReadMessageBatchResult(
        IReadOnlyList<IMessageContext> Messages,
        long StreamVersion,
        long EndingStreamPosition,
        long EndingGlobalPosition
    );

    private readonly record struct NoMessages : IMessageBatchResult
    {
        public static readonly NoMessages Instance = new();
    }

    private readonly record struct Success(long StreamPosition, long GlobalPosition) : IMessageBatchResult;

    private readonly record struct Error(long StreamPosition, Exception Exception) : IMessageBatchResult;

    public sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance { get; } = new();

        public void Dispose()
        {
            //no-op
        }
    }
}

public static partial class Log
{
    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Processing checkpoint {CheckpointId} at position {StreamPosition} and version {StreamVersion}"
    )]
    public static partial void ProcessingCheckpoint(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        long streamVersion
    );

    [LoggerMessage(0, LogLevel.Trace, "Handling message {MessageId} for checkpoint {CheckpointId}")]
    public static partial void HandlingMessageForCheckpoint(this ILogger logger, string messageId, long checkpointId);

    [LoggerMessage(0, LogLevel.Trace, "Found {Count} messages to process for checkpoint {CheckpointId}")]
    public static partial void FoundMessagesToProcessForCheckpoint(this ILogger logger, int count, long checkpointId);

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Checkpoint {CheckpointId} processed successfully up to position {StreamPosition}"
    )]
    public static partial void CheckpointProcessedSuccessfully(
        this ILogger logger,
        long checkpointId,
        long streamPosition
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Retry attempt {Attempt} succeeded for checkpoint {CheckpointId} at position {StreamPosition}"
    )]
    public static partial void RetryAttemptSucceeded(
        this ILogger logger,
        int attempt,
        long checkpointId,
        long streamPosition
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Checkpoint {CheckpointId} encountered an error at position {StreamPosition} - will start retrying at {RetryAt}"
    )]
    public static partial void CheckpointWillStartRetry(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        DateTimeOffset? retryAt
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Checkpoint {CheckpointId} encountered an error at position {StreamPosition} - max retry count is set to zero for exception type {ExceptionType}, setting it to failed immediately"
    )]
    public static partial void CheckpointWillMoveToFailedImmediately(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        Type exceptionType
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Retry attempt failed for checkpoint {CheckpointId} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - scheduling retry at {RetryAt}"
    )]
    public static partial void RetryAttemptFailedWillTryAgain(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        int attempt,
        int maxRetryCount,
        DateTimeOffset? retryAt
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Retry failed for checkpoint {CheckpointId} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - max retries exceeded, setting checkpoint status to Failed"
    )]
    public static partial void RetryAttemptFailedMovingCheckpointToFailed(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        int attempt,
        int maxRetryCount
    );
}
