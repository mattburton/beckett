using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Messages;
using Beckett.Subscriptions.Retries;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions;

public class SubscriptionStreamProcessor(
    IPostgresDatabase database,
    IPostgresMessageDeserializer messageDeserializer,
    IMessageStore messageStore,
    IServiceProvider serviceProvider,
    SubscriptionOptions options,
    ILogger<SubscriptionStreamProcessor> logger
) : ISubscriptionStreamProcessor
{
    public async Task Process(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Subscription subscription,
        string topic,
        string streamId,
        long streamPosition,
        int batchSize,
        bool retryOnError,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(
            new ReadStream(
                topic,
                streamId,
                new ReadOptions
                {
                    StartingStreamPosition = streamPosition,
                    Count = batchSize
                }
            ),
            connection,
            cancellationToken
        );

        var messages = new List<IMessageContext>();

        foreach (var streamMessage in streamMessages)
        {
            var (type, message, metadata) = messageDeserializer.DeserializeAll(streamMessage);

            messages.Add(new MessageContext(
                streamMessage.Id,
                streamMessage.Topic,
                streamMessage.StreamId,
                streamMessage.StreamPosition,
                streamMessage.GlobalPosition,
                type,
                message,
                metadata,
                streamMessage.Timestamp,
                messageStore,
                serviceProvider
            ));
        }

        var result = await HandleMessageBatch(subscription, topic, streamId, messages, true, cancellationToken);

        switch (result)
        {
            case MessageBatchResult.Success success:
                await database.Execute(
                    new UpdateCheckpointStreamPosition(
                        options.ApplicationName,
                        subscription.Name,
                        topic,
                        streamId,
                        success.StreamPosition,
                        false
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                break;
            case MessageBatchResult.Blocked blocked:
                if (ShouldThrow(subscription, retryOnError))
                {
                    throw blocked.Exception;
                }

                await database.Execute(
                    new UpdateCheckpointStreamPosition(
                        options.ApplicationName,
                        subscription.Name,
                        topic,
                        streamId,
                        blocked.StreamPosition,
                        true
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                break;
        }
    }

    private static bool ShouldThrow(Subscription subscription, bool retryOnError)
    {
        return !retryOnError || subscription.MaxRetryCount == 0;
    }

    private async Task<MessageBatchResult> HandleMessageBatch(
        Subscription subscription,
        string topic,
        string streamId,
        IReadOnlyList<IMessageContext> messages,
        bool retryOnError,
        CancellationToken cancellationToken
    )
    {
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
                    if (subscription.StaticMethod != null)
                    {
                        await subscription.StaticMethod!(messageContext, cancellationToken);

                        continue;
                    }

                    using var scope = serviceProvider.CreateScope();

                    var handler = scope.ServiceProvider.GetRequiredService(subscription.Type);

                    if (subscription.AcceptsMessageContext)
                    {
                        await subscription.InstanceMethod!(handler, messageContext, cancellationToken);
                    }
                    else
                    {
                        await subscription.InstanceMethod!(handler, messageContext.Message, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching message {MessageType} to subscription {SubscriptionType} for stream {Topic}-{StreamId} using handler {HandlerType} [ID: {MessageId}]",
                        messageContext.Type,
                        subscription.Name,
                        topic,
                        streamId,
                        subscription.Type,
                        messageContext.Id
                    );

                    if (!retryOnError || subscription.MaxRetryCount == 0)
                    {
                        throw;
                    }

                    var retryAt = 0.GetNextDelayWithExponentialBackoff();

                    await messageStore.AppendToStream(
                        RetryConstants.Topic,
                        RetryStreamId.For(
                            subscription.Name,
                            topic,
                            streamId,
                            messageContext.StreamPosition
                        ),
                        ExpectedVersion.Any,
                        new SubscriptionError(
                            subscription.Name,
                            topic,
                            streamId,
                            messageContext.StreamPosition,
                            ExceptionData.From(e),
                            retryAt,
                            DateTimeOffset.UtcNow
                        ).ScheduleAt(retryAt),
                        cancellationToken
                    );

                    return new MessageBatchResult.Blocked(messageContext.StreamPosition, e);
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

    public abstract record MessageBatchResult
    {
        public record NoMessages : MessageBatchResult;

        public record Success(long StreamPosition) : MessageBatchResult;

        public record Blocked(long StreamPosition, Exception Exception) : MessageBatchResult;
    }
}
