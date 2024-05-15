using System.Transactions;
using Beckett.Messages;
using Beckett.Messages.Scheduling;
using Beckett.OpenTelemetry;

namespace Beckett;

public interface IMessageStore
{
    Task<AppendResult> AppendToStream(
        string topic,
        object streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string topic,
        object streamId,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}

public class MessageStore(
    IMessageStorage messageStorage,
    IMessageScheduler messageScheduler,
    IInstrumentation instrumentation
) : IMessageStore
{
    public async Task<AppendResult> AppendToStream(
        string topic,
        object streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    )
    {
        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartAppendToStreamActivity(topic, streamId, metadata);

        var messagesToAppend = new List<MessageEnvelope>();
        var messagesToSchedule = new List<ScheduledMessageEnvelope>();

        foreach (var message in messages)
        {
            var messageMetadata = new Dictionary<string, object>(metadata);

            if (message is MessageMetadataWrapper messageWithMetadata)
            {
                foreach (var item in messageWithMetadata.Metadata)
                {
                    messageMetadata.TryAdd(item.Key, item.Value);
                }

                if (message is not ScheduledMessageWrapper)
                {
                    messagesToAppend.Add(new MessageEnvelope(messageWithMetadata.Message, messageMetadata));

                    continue;
                }
            }

            if (message is ScheduledMessageWrapper scheduledMessage)
            {
                messagesToSchedule.Add(
                    new ScheduledMessageEnvelope(scheduledMessage.Message, messageMetadata, scheduledMessage.DeliverAt)
                );

                continue;
            }

            messagesToAppend.Add(new MessageEnvelope(message, messageMetadata));
        }

        var streamIdString = streamId.ToString()!;

        if (messagesToSchedule.Count == 0)
        {
            return await messageStorage.AppendToStream(
                topic,
                streamIdString,
                expectedVersion,
                messagesToAppend,
                cancellationToken
            );
        }

        if (messagesToAppend.Count == 0)
        {
            await messageScheduler.ScheduleMessages(topic, streamIdString, messagesToSchedule, cancellationToken);

            return new AppendResult(-1);
        }

        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        await messageScheduler.ScheduleMessages(topic, streamIdString, messagesToSchedule, cancellationToken);

        var result = await messageStorage.AppendToStream(
            topic,
            streamIdString,
            expectedVersion,
            messagesToAppend,
            cancellationToken
        );

        transactionScope.Complete();

        return result;
    }

    public Task<ReadResult> ReadStream(
        string topic,
        object streamId,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        using var activity = instrumentation.StartReadStreamActivity(topic, streamId);

        return messageStorage.ReadStream(topic, streamId.ToString()!, options, cancellationToken);
    }
}

public static class MessageStoreExtensions
{
    public static Task<AppendResult> AppendToStream(
        this IMessageStore messageStore,
        string topic,
        object streamId,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    )
    {
        return messageStore.AppendToStream(topic, streamId, expectedVersion, [message], cancellationToken);
    }

    public static Task<ReadResult> ReadStream(
        this IMessageStore messageStore,
        string topic,
        object streamId,
        CancellationToken cancellationToken
    )
    {
        return messageStore.ReadStream(topic, streamId, ReadOptions.Default, cancellationToken);
    }
}
