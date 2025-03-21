using Beckett.Database;
using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.OpenTelemetry;
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
    BeckettOptions options,
    IInstrumentation instrumentation,
    ILogger<CheckpointProcessor> logger
) : ICheckpointProcessor
{
    public async Task Process(
        int instance,
        Checkpoint checkpoint,
        Subscription subscription,
        CancellationToken cancellationToken
    )
    {
        using var checkpointLoggingScope = CheckpointLoggingScope(instance, checkpoint);

        logger.ProcessingCheckpoint(checkpoint.Id, checkpoint.StreamPosition, checkpoint.StreamVersion);

        var result = await HandleMessageBatch(
            checkpoint,
            subscription,
            options.Subscriptions.SubscriptionStreamBatchSize,
            cancellationToken
        );

        switch (result)
        {
            case Success success:
                //if host is stopping we might not have processed anything yet for the current batch so exit early
                if (cancellationToken.IsCancellationRequested && !checkpoint.IsRetryOrFailure &&
                    success.StreamPosition == checkpoint.StreamPosition)
                {
                    break;
                }

                if (checkpoint.ParentId.HasValue)
                {
                    await using var connection = dataSource.CreateConnection();

                    await connection.OpenAsync(cancellationToken);

                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                    var parentStreamVersion = await database.Execute(
                        new ReserveCheckpoint(
                            checkpoint.ParentId.Value,
                            options.Subscriptions.ReservationTimeout,
                            options.Postgres
                        ),
                        connection,
                        transaction,
                        cancellationToken
                    );

                    if (parentStreamVersion == null)
                    {
                        await database.Execute(
                            new UpdateChildCheckpointPosition(
                                checkpoint.Id,
                                success.StreamPosition,
                                null,
                                DateTimeOffset.UtcNow,
                                options.Postgres
                            ),
                            connection,
                            transaction,
                            cancellationToken
                        );

                        await transaction.CommitAsync(cancellationToken);

                        throw new Exception(
                            "Unable to reserve parent checkpoint after processing child checkpoint successfully - will try again later"
                        );
                    }

                    //check if there are any new messages to process in the stream
                    var stream = await messageStorage.ReadStream(
                        checkpoint.StreamName,
                        new ReadStreamOptions
                        {
                            StartingStreamPosition = success.StreamPosition + 1,
                            EndingGlobalPosition = parentStreamVersion.Value,
                            Types = subscription.MessageTypeNames.ToArray(),
                            Count = 1
                        },
                        cancellationToken
                    );

                    long? streamVersion = stream.StreamMessages.Count > 0
                        ? stream.StreamMessages[^1].StreamPosition
                        : null;

                    await database.Execute(
                        new UpdateChildCheckpointPosition(
                            checkpoint.Id,
                            success.StreamPosition,
                            streamVersion,
                            //if there are new messages to process we set the processAt to now so we can process them immediately
                            streamVersion.HasValue ? DateTimeOffset.UtcNow : null,
                            options.Postgres
                        ),
                        connection,
                        transaction,
                        cancellationToken
                    );

                    await database.Execute(
                        new ReleaseCheckpointReservation(checkpoint.ParentId.Value, options.Postgres),
                        connection,
                        transaction,
                        cancellationToken
                    );

                    await transaction.CommitAsync(cancellationToken);

                    break;
                }

                await database.Execute(
                    new UpdateCheckpointPosition(checkpoint.Id, success.StreamPosition, null, options.Postgres),
                    cancellationToken
                );

                SuccessTraceLogging(checkpoint, success);

                break;
            case Error error:
                var exceptionType = error.Exception.GetType();

                var maxRetryCount = subscription.GetMaxRetryCount(options, exceptionType);

                var attempt = checkpoint.IsRetryOrFailure ? checkpoint.RetryAttempts : 0;

                var status = attempt >= maxRetryCount ? CheckpointStatus.Failed : CheckpointStatus.Retry;

                DateTimeOffset? processAt = status == CheckpointStatus.Retry
                    ? attempt.GetNextDelayWithExponentialBackoff(
                        options.Subscriptions.Retries.InitialDelay,
                        options.Subscriptions.Retries.MaxDelay
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
                        processAt,
                        options.Postgres
                    ),
                    cancellationToken
                );

                break;
        }
    }

    private async Task<IMessageBatchResult> HandleMessageBatch(
        Checkpoint checkpoint,
        Subscription subscription,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        if (ChildCheckpointCanBeClearedWithoutProcessing(checkpoint))
        {
            return new Success(checkpoint.StreamPosition);
        }

        IReadOnlyList<IMessageContext> messageBatch;

        try
        {
            messageBatch = await ReadMessageBatch(checkpoint, subscription, batchSize, cancellationToken);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error reading message batch for subscription {SubscriptionType} for stream {StreamName} at {StreamPosition} [Checkpoint: {CheckpointId}]",
                subscription.Name,
                checkpoint.StreamName,
                checkpoint.StartingPositionFor(subscription),
                checkpoint.Id
            );

            return new Error(checkpoint.StartingPositionFor(subscription), e);
        }

        try
        {
            if (messageBatch.Count == 0)
            {
                if (checkpoint.IsGlobalScoped(subscription) && checkpoint.StreamPosition < checkpoint.StreamVersion)
                {
                    return new Success(checkpoint.StreamVersion);
                }

                return NoMessages.Instance;
            }

            if (subscription.Handler.IsBatchHandler)
            {
                try
                {
                    return await DispatchMessageBatchToHandler(
                        checkpoint,
                        subscription,
                        messageBatch,
                        cancellationToken
                    );
                }
                catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching message batch to subscription {SubscriptionName} for stream {StreamName} [Checkpoint: {CheckpointId}]",
                        subscription.Name,
                        checkpoint.StreamName,
                        checkpoint.Id
                    );

                    return new Error(messageBatch[0].PositionFor(subscription, checkpoint), e);
                }
            }

            var lastProcessedStreamPosition = checkpoint.StreamPosition;

            foreach (var streamMessage in messageBatch.Where(x => subscription.SubscribedToMessage(x.Type)))
            {
                try
                {
                    //if host is stopping attempt to record where we left off
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new Success(lastProcessedStreamPosition);
                    }

                    await DispatchMessageToHandler(checkpoint, subscription, streamMessage, cancellationToken);

                    lastProcessedStreamPosition = streamMessage.PositionFor(subscription, checkpoint);
                }
                catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
                {
                    throw;
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

                    return new Error(streamMessage.PositionFor(subscription, checkpoint), e);
                }
            }

            return new Success(messageBatch[^1].PositionFor(subscription, checkpoint));
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception processing checkpoint {CheckpointId}", checkpoint.Id);

            throw;
        }
    }

    private bool ChildCheckpointCanBeClearedWithoutProcessing(Checkpoint checkpoint)
    {
        var result = checkpoint is { ParentId: not null, IsRetryOrFailure: true } &&
               checkpoint.StreamPosition == checkpoint.StreamVersion;

        if (result)
        {
            logger.LogInformation("Child checkpoint can be cleared without processing [Checkpoint ID: {CheckpointId}, Parent ID: {ParentId}]", checkpoint.Id, checkpoint.ParentId);
        }

        return result;
    }

    private async Task DispatchMessageToHandler(
        Checkpoint checkpoint,
        Subscription subscription,
        IMessageContext messageContext,
        CancellationToken cancellationToken
    )
    {
        using var activity = instrumentation.StartHandleMessageActivity(subscription, messageContext);

        using var messageLoggingScope = MessageLoggingScope(messageContext);

        logger.HandlingMessageForCheckpoint(messageContext.Id, checkpoint.Id);

        using var scope = serviceProvider.CreateScope();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        cts.CancelAfter(options.Subscriptions.ReservationTimeout);

        try
        {
            await subscription.Handler.Invoke(messageContext, checkpoint.ToContext(), scope.ServiceProvider, cts.Token);
        }
        //special handling for HttpClient timeouts - see https://github.com/dotnet/runtime/issues/21965
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException timeoutException)
        {
            throw timeoutException;
        }
        catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Shutdown signal received while processing message");

                throw new OperationCanceledException(e.Message, e, cancellationToken);
            }

            throw new TimeoutException(
                $"Handler exceeded the reservation timeout of {options.Subscriptions.ReservationTimeout.TotalSeconds} seconds while handling message {messageContext.Id} for checkpoint {checkpoint.Id}",
                e
            );
        }
    }

    private async Task<IMessageBatchResult> DispatchMessageBatchToHandler(
        Checkpoint checkpoint,
        Subscription subscription,
        IReadOnlyList<IMessageContext> messages,
        CancellationToken cancellationToken
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        cts.CancelAfter(options.Subscriptions.ReservationTimeout);

        try
        {
            var messageBatch = messages.ToList();

            using var scope = serviceProvider.CreateScope();

            await subscription.Handler.Invoke(messageBatch, checkpoint.ToContext(), scope.ServiceProvider, cts.Token);

            return new Success(messageBatch[^1].PositionFor(subscription, checkpoint));
        }
        //special handling for HttpClient timeouts - see https://github.com/dotnet/runtime/issues/21965
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException timeoutException)
        {
            throw timeoutException;
        }
        catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Shutdown signal received while processing message");

                throw new OperationCanceledException(e.Message, e, cancellationToken);
            }

            throw new TimeoutException(
                $"Handler exceeded the reservation timeout of {options.Subscriptions.ReservationTimeout.TotalSeconds} seconds while handling message batch starting with {messages[0].Id} for checkpoint {checkpoint.Id}",
                e
            );
        }
    }

    private async Task<IReadOnlyList<IMessageContext>> ReadMessageBatch(
        Checkpoint checkpoint,
        Subscription subscription,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startingStreamPosition = checkpoint.StartingPositionFor(subscription);
        var endingStreamPosition = checkpoint.StreamVersion;
        var skipBatchSizeCheck = false;

        if (checkpoint.IsRetryOrFailure)
        {
            startingStreamPosition = checkpoint.RetryStartingPositionFor(subscription);

            if (!subscription.Handler.IsBatchHandler)
            {
                endingStreamPosition = startingStreamPosition + 1;

                skipBatchSizeCheck = true;
            }
        }

        if (!skipBatchSizeCheck)
        {
            var count = endingStreamPosition - checkpoint.StreamPosition;

            if (count > batchSize)
            {
                endingStreamPosition = checkpoint.StreamPosition + batchSize;
            }
        }

        if (checkpoint.IsGlobalScoped(subscription))
        {
            var globalStream = await messageStorage.ReadGlobalStream(
                new ReadGlobalStreamOptions
                {
                    StartingGlobalPosition = startingStreamPosition,
                    Count = batchSize,
                    Types = subscription.MessageTypeNames.ToArray()
                },
                cancellationToken
            );

            logger.FoundMessagesToProcessForCheckpoint(globalStream.StreamMessages.Count, checkpoint.Id);

            return globalStream.StreamMessages.Select(MessageContext.From).ToList();
        }

        var stream = await messageStorage.ReadStream(
            checkpoint.StreamName,
            new ReadStreamOptions
            {
                StartingStreamPosition = startingStreamPosition,
                EndingStreamPosition = endingStreamPosition
            },
            cancellationToken
        );

        logger.FoundMessagesToProcessForCheckpoint(stream.StreamMessages.Count, checkpoint.Id);

        return stream.StreamMessages.Select(MessageContext.From).ToList();
    }

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

    private readonly record struct NoMessages : IMessageBatchResult
    {
        public static readonly NoMessages Instance = new();
    }

    private readonly record struct Success(long StreamPosition) : IMessageBatchResult;

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

public static class MessageContextExtensions
{
    public static long PositionFor(this IMessageContext context, Subscription subscription, Checkpoint checkpoint)
    {
        return subscription.StreamScope == StreamScope.PerStream || checkpoint.ParentId.HasValue
            ? context.StreamPosition
            : context.GlobalPosition;
    }
}
