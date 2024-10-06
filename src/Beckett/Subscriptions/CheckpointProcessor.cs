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
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        bool isRetry,
        CancellationToken cancellationToken
    )
    {
        var result = await HandleMessageBatch(subscription, streamName, streamPosition, batchSize, cancellationToken);

        switch (result)
        {
            case MessageBatchResult.Success success:
                if (isRetry)
                {
                    return;
                }

                await database.Execute(
                    new UpdateCheckpointStatus(
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        streamName,
                        success.StreamPosition,
                        CheckpointStatus.Active
                    ),
                    cancellationToken
                );

                logger.LogTrace(
                    "Checkpoint {GroupName}:{Name}:{StreamName} processed successfully up to position {StreamPosition}",
                    subscription.Name,
                    options.Subscriptions.GroupName,
                    streamName,
                    success.StreamPosition
                );

                break;
            case MessageBatchResult.Error error:
                if (isRetry)
                {
                    throw error.Exception;
                }

                await database.Execute(
                    new RecordCheckpointError(
                        options.Subscriptions.GroupName,
                        subscription.Name,
                        streamName,
                        error.StreamPosition,
                        ExceptionData.From(error.Exception).ToJson()
                    ),
                    cancellationToken
                );

                break;
        }
    }

    private async Task<MessageBatchResult> HandleMessageBatch(
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        List<MessageContext> messages;

        try
        {
            messages = await ReadMessageBatch(
                subscription,
                streamName,
                streamPosition,
                batchSize,
                cancellationToken
            );
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error reading message batch for subscription {SubscriptionType} for stream {StreamName} at {StreamPosition}",
                subscription.Name,
                streamName,
                streamPosition
            );

            return new MessageBatchResult.Error(streamPosition, e);
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
                            $"Subscription handler type is not configured for {subscription.Name}"
                        );
                    }

                    if (subscription.InstanceMethod == null)
                    {
                        throw new InvalidOperationException(
                            $"Subscription handler expression is not configured for {subscription.Name}"
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
                        "Error dispatching message {MessageType} to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [ID: {MessageId}]",
                        messageContext.Type,
                        subscription.Name,
                        streamName,
                        subscription.HandlerName,
                        messageContext.Id
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
            logger.LogError(e, "Unhandled exception processing subscription stream");

            throw;
        }
    }

    private async Task<List<MessageContext>> ReadMessageBatch(
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStorage.ReadStream(
            streamName,
            new ReadStreamOptions
            {
                StartingStreamPosition = streamPosition,
                Count = batchSize
            },
            cancellationToken
        );

        logger.LogTrace(
            "Found {Count} messages to process for checkpoint {GroupName}:{Name}:{StreamName}",
            stream.Messages.Count,
            subscription.Name,
            options.Subscriptions.GroupName,
            streamName
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

    public abstract record MessageBatchResult
    {
        public record NoMessages : MessageBatchResult;

        public record Success(long StreamPosition) : MessageBatchResult;

        public record Error(long StreamPosition, Exception Exception) : MessageBatchResult;
    }
}
