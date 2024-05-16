using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Subscriptions.Models;
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
    IInstrumentation instrumentation,
    ILogger<SubscriptionStreamProcessor> logger
) : ISubscriptionStreamProcessor
{
    public async Task Process(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        bool moveToFailedOnError,
        bool throwOnError,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(
            new ReadStream(
                streamName,
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
                streamMessage.StreamName,
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

        var result = await HandleMessageBatch(subscription, streamName, messages, cancellationToken);

        switch (result)
        {
            case MessageBatchResult.Success success:
                await database.Execute(
                    new UpdateCheckpointStatus(
                        options.ApplicationName,
                        subscription.Name,
                        streamName,
                        success.StreamPosition,
                        CheckpointStatus.Active
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                break;
            case MessageBatchResult.Error error:
                if (moveToFailedOnError)
                {
                    await database.Execute(
                        new UpdateCheckpointStatus(
                            options.ApplicationName,
                            subscription.Name,
                            streamName,
                            error.StreamPosition,
                            CheckpointStatus.Failed
                        ),
                        connection,
                        transaction,
                        cancellationToken
                    );
                }

                if (throwOnError)
                {
                    throw error.Exception;
                }

                if (moveToFailedOnError)
                {
                    return;
                }

                await StartRetryForSubscriptionStream(subscription, streamName, error, cancellationToken);

                await database.Execute(
                    new UpdateCheckpointStatus(
                        options.ApplicationName,
                        subscription.Name,
                        streamName,
                        error.StreamPosition,
                        CheckpointStatus.Retry
                    ),
                    connection,
                    transaction,
                    cancellationToken
                );

                break;
        }
    }

    private async Task StartRetryForSubscriptionStream(
        Subscription subscription,
        string streamName,
        MessageBatchResult.Error error,
        CancellationToken cancellationToken
    )
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(
                subscription.Name,
                streamName,
                error.StreamPosition
            ),
            ExpectedVersion.Any,
            new SubscriptionError(
                subscription.Name,
                streamName,
                error.StreamPosition,
                ExceptionData.From(error.Exception),
                DateTimeOffset.UtcNow
            ).DelayFor(TimeSpan.FromSeconds(10)),
            cancellationToken
        );
    }

    private async Task<MessageBatchResult> HandleMessageBatch(
        Subscription subscription,
        string streamName,
        IReadOnlyList<IMessageContext> messages,
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
                    using var activity = instrumentation.StartHandleMessageActivity(subscription, messageContext);

                    if (subscription.HasStaticMethod)
                    {
                        if (subscription.AcceptsMessageContext)
                        {
                            await subscription.StaticMethod!(messageContext, cancellationToken);
                        }
                        else
                        {
                            await subscription.StaticMethod!(messageContext.Message, cancellationToken);
                        }

                        continue;
                    }

                    using var scope = serviceProvider.CreateScope();

                    var handler = scope.ServiceProvider.GetRequiredService(subscription.Type!);

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
                        "Error dispatching message {MessageType} to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [ID: {MessageId}]",
                        messageContext.Type,
                        subscription.Name,
                        streamName,
                        subscription.Type,
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

    public abstract record MessageBatchResult
    {
        public record NoMessages : MessageBatchResult;

        public record Success(long StreamPosition) : MessageBatchResult;

        public record Error(long StreamPosition, Exception Exception) : MessageBatchResult;
    }
}
