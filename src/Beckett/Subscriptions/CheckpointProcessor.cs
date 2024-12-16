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

        var startingStreamPosition = checkpoint.StreamPosition + 1;
        var batchSize = options.Subscriptions.SubscriptionStreamBatchSize;

        if (checkpoint.IsRetryOrFailure)
        {
            startingStreamPosition = checkpoint.StreamPosition;

            if (!subscription.BatchHandler)
            {
                batchSize = 1;
            }
        }

        var result = await HandleMessageBatch(
            checkpoint,
            subscription,
            checkpoint.StreamName,
            startingStreamPosition,
            batchSize,
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

                await database.Execute(
                    new UpdateCheckpointPosition(
                        checkpoint.Id,
                        success.StreamPosition,
                        null,
                        options.Postgres
                    ),
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
        string streamName,
        long startingStreamPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        List<MessageContext> messages;

        try
        {
            messages = await ReadMessageBatch(
                checkpoint,
                streamName,
                startingStreamPosition,
                batchSize,
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
                "Error reading message batch for subscription {SubscriptionType} for stream {StreamName} at {StreamPosition} [Checkpoint: {CheckpointId}]",
                subscription.Name,
                streamName,
                startingStreamPosition,
                checkpoint.Id
            );

            return new Error(startingStreamPosition, e);
        }

        try
        {
            if (messages.Count == 0)
            {
                return NoMessages.Instance;
            }

            if (subscription.BatchHandler)
            {
                return await DispatchMessageBatchToHandler(
                    checkpoint,
                    subscription,
                    streamName,
                    messages,
                    cancellationToken
                );
            }

            var lastProcessedStreamPosition = checkpoint.StreamPosition;

            foreach (var messageContext in messages.Where(x => subscription.SubscribedToMessage(x.Type)))
            {
                try
                {
                    //if host is stopping attempt to record where we left off
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new Success(lastProcessedStreamPosition);
                    }

                    await DispatchMessageToHandler(checkpoint, subscription, messageContext, cancellationToken);

                    lastProcessedStreamPosition = messageContext.StreamPosition;
                }
                catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching message {MessageType} to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [Message ID: {MessageId}, Checkpoint: {CheckpointId}]",
                        messageContext.Type,
                        subscription.Name,
                        streamName,
                        subscription.HandlerName,
                        messageContext.Id,
                        checkpoint.Id
                    );

                    return new Error(messageContext.StreamPosition, e);
                }
            }

            return new Success(messages[^1].StreamPosition);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Unhandled exception processing checkpoint {CheckpointId}",
                checkpoint.Id
            );

            throw;
        }
    }

    private async Task DispatchMessageToHandler(
        Checkpoint checkpoint,
        Subscription subscription,
        MessageContext messageContext,
        CancellationToken cancellationToken
    )
    {
        using var activity = instrumentation.StartHandleMessageActivity(subscription, messageContext);

        using var messageLoggingScope = MessageLoggingScope(messageContext);

        logger.HandlingMessageForCheckpoint(messageContext.Id, checkpoint.Id);

        if (subscription.HandlerType == null)
        {
            throw new InvalidOperationException(
                $"Subscription handler type is not configured for {subscription.Name} [Checkpoint: {checkpoint.Id}]"
            );
        }

        using var scope = serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService(subscription.HandlerType);

        switch (handler)
        {
            case IMessageHandler instance:
                await instance.Handle(messageContext, cancellationToken);
                break;
            case IMessageHandlerAdapter adapter:
                await adapter.Handle(messageContext, cancellationToken);
                break;
        }
    }

    private async Task<IMessageBatchResult> DispatchMessageBatchToHandler(
        Checkpoint checkpoint,
        Subscription subscription,
        string streamName,
        List<MessageContext> messages,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var messageBatch = messages.Where(x => subscription.SubscribedToMessage(x.Type)).Cast<IMessageContext>()
                .ToList();

            if (subscription.HandlerType == null)
            {
                throw new InvalidOperationException(
                    $"Subscription batch handler type is not configured for {subscription.Name} [Checkpoint: {checkpoint.Id}]"
                );
            }

            using var scope = serviceProvider.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService(subscription.HandlerType);

            if (handler is not IMessageBatchHandler batchHandler)
            {
                throw new InvalidOperationException(
                    $"Invalid batch handler type for {subscription.Name} [Checkpoint: {checkpoint.Id}]"
                );
            }

            await batchHandler.Handle(messageBatch, cancellationToken);

            return new Success(messages[^1].StreamPosition);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error dispatching message batch to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [Checkpoint: {CheckpointId}]",
                subscription.Name,
                streamName,
                subscription.HandlerName,
                checkpoint.Id
            );

            return new Error(messages[0].StreamPosition, e);
        }
    }

    private async Task<List<MessageContext>> ReadMessageBatch(
        Checkpoint checkpoint,
        string streamName,
        long startingStreamPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stream = await messageStorage.ReadStream(
            streamName,
            new ReadStreamOptions
            {
                StartingStreamPosition = startingStreamPosition,
                Count = batchSize
            },
            cancellationToken
        );

        logger.FoundMessagesToProcessForCheckpoint(stream.Messages.Count, checkpoint.Id);

        return stream.Messages.Select(
            message => new MessageContext(
                message.Id,
                message.StreamName,
                message.StreamPosition,
                message.GlobalPosition,
                message.Type,
                message.Data,
                message.Metadata,
                message.Timestamp
            )
        ).ToList();
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

    private IDisposable? MessageLoggingScope(MessageContext messageContext)
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
