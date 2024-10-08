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
        Checkpoint checkpoint,
        Subscription subscription,
        CancellationToken cancellationToken
    )
    {
        var startingStreamPosition = checkpoint.StreamPosition + 1;
        var batchSize = options.Subscriptions.SubscriptionStreamBatchSize;

        if (checkpoint.IsRetryOrFailure)
        {
            startingStreamPosition = checkpoint.StreamPosition;
            batchSize = 1;
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
            case MessageBatchResult.Success success:
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
            case MessageBatchResult.Error error:
                var exceptionType = error.Exception.GetType();

                var maxRetryCount = subscription.GetMaxRetryCount(options, exceptionType);

                var attempt = checkpoint.IsRetryOrFailure ? checkpoint.RetryAttempts : 0;

                var status = attempt >= maxRetryCount ? CheckpointStatus.Failed : CheckpointStatus.Retry;

                DateTimeOffset? processAt = status == CheckpointStatus.Retry ? attempt.GetNextDelayWithExponentialBackoff(
                    options.Subscriptions.Retries.InitialDelay,
                    options.Subscriptions.Retries.MaxDelay
                ) : null;

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

    private async Task<MessageBatchResult> HandleMessageBatch(
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

            return new MessageBatchResult.Error(startingStreamPosition, e);
        }

        try
        {
            if (messages.Count == 0)
            {
                return new MessageBatchResult.NoMessages();
            }

            foreach (var messageContext in messages)
            {
                if (!subscription.SubscribedToMessage(messageContext.Type))
                {
                    continue;
                }

                try
                {
                    using var activity = instrumentation.StartHandleMessageActivity(subscription, messageContext);

                    if (subscription.StaticMethod != null)
                    {
                        await subscription.StaticMethod(messageContext, cancellationToken);

                        continue;
                    }

                    if (subscription.HandlerType == null)
                    {
                        throw new InvalidOperationException(
                            $"Subscription handler type is not configured for {subscription.Name} [Checkpoint: {checkpoint.Id}]"
                        );
                    }

                    if (subscription.InstanceMethod == null)
                    {
                        throw new InvalidOperationException(
                            $"Subscription handler expression is not configured for {subscription.Name} [Checkpoint: {checkpoint.Id}]"
                        );
                    }

                    using var scope = serviceProvider.CreateScope();

                    var handler = scope.ServiceProvider.GetRequiredService(subscription.HandlerType);

                    IMessageContext messageContextForScope = messageContext with { Services = scope.ServiceProvider };

                    await subscription.InstanceMethod(handler, messageContextForScope, cancellationToken);
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

                    return new MessageBatchResult.Error(messageContext.StreamPosition, e);
                }
            }

            return new MessageBatchResult.Success(messages[^1].StreamPosition);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Unhandled exception processing subscription stream [Checkpoint: {CheckpointId}]",
                checkpoint.Id
            );

            throw;
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
        var stream = await messageStorage.ReadStream(
            streamName,
            new ReadStreamOptions
            {
                StartingStreamPosition = startingStreamPosition,
                Count = batchSize
            },
            cancellationToken
        );

        logger.LogTrace(
            "Found {Count} messages to process for checkpoint {Id}",
            stream.Messages.Count,
            checkpoint.Id
        );

        var messages = new List<MessageContext>();

        foreach (var message in stream.Messages)
        {
            messages.Add(
                new MessageContext(
                    message.Id,
                    message.StreamName,
                    message.StreamPosition,
                    message.GlobalPosition,
                    message.Type,
                    message.Data,
                    message.Metadata,
                    message.Timestamp,
                    serviceProvider
                )
            );
        }

        return messages;
    }

    private void SuccessTraceLogging(Checkpoint checkpoint, MessageBatchResult.Success success)
    {
        if (!logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        if (checkpoint.Status == CheckpointStatus.Active)
        {
            logger.LogTrace(
                "Checkpoint {CheckpointId} processed successfully up to position {StreamPosition}",
                checkpoint.Id,
                success.StreamPosition
            );
        }
        else
        {
            logger.LogTrace(
                "Retry attempt {Attempt} succeeded for checkpoint {CheckpointId} at position {StreamPosition}",
                checkpoint.RetryAttempts + 1,
                checkpoint.Id,
                success.StreamPosition
            );
        }
    }

    private void ErrorTraceLogging(
        Checkpoint checkpoint,
        CheckpointStatus status,
        MessageBatchResult.Error error,
        DateTimeOffset? processAt,
        Type exceptionType,
        int attempt,
        int maxRetryCount
    )
    {
        if (!logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        if (checkpoint.Status == CheckpointStatus.Active)
        {
            if (status == CheckpointStatus.Retry)
            {
                logger.LogTrace(
                    "Checkpoint {CheckpointId} encountered an error at position {StreamPosition} - will start retrying at {RetryAt}",
                    checkpoint.Id,
                    error.StreamPosition,
                    processAt
                );
            }
            else
            {
                logger.LogTrace(
                    "Checkpoint {CheckpointId} encountered an error at position {StreamPosition} - max retry count is set to zero for exception type {ExceptionType}, setting it to failed immediately",
                    checkpoint.Id,
                    error.StreamPosition,
                    exceptionType
                );
            }

            return;
        }

        if (status == CheckpointStatus.Retry)
        {
            logger.LogTrace(
                "Retry attempt failed for checkpoint {CheckpointId} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - scheduling retry at {RetryAt}",
                checkpoint.Id,
                error.StreamPosition,
                attempt,
                maxRetryCount,
                processAt
            );
        }
        else
        {
            logger.LogTrace(
                "Retry failed for checkpoint {CheckpointId} at position {StreamPosition} - attempt {Attempt} of {MaxRetryCount} - max retries exceeded, setting checkpoint status to Failed",
                checkpoint.Id,
                error.StreamPosition,
                attempt,
                maxRetryCount
            );
        }
    }

    public abstract record MessageBatchResult
    {
        public record NoMessages : MessageBatchResult;

        public record Success(long StreamPosition) : MessageBatchResult;

        public record Error(long StreamPosition, Exception Exception) : MessageBatchResult;
    }
}
