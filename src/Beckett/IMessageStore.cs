using System.Transactions;
using Beckett.Messages;
using Beckett.Messages.Scheduling;

namespace Beckett;

public interface IMessageStore
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}

public class MessageStore(IMessageStorage messageStorage, IMessageScheduler messageScheduler) : IMessageStore
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    )
    {
        //TODO - populate from activity source
        var metadata = new Dictionary<string, object>();
        var messagesToAppend = new List<MessageEnvelope>();
        var messagesToSchedule = new List<ScheduledMessageEnvelope>();

        foreach (var message in messages)
        {
            var messageMetadata = new Dictionary<string, object>(metadata);

            if (message is MessageMetadataWrapper messageWithMetadata)
            {
                foreach (var item in messageWithMetadata.Metadata)
                {
                    messageMetadata.Add(item.Key, item.Value);
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

        if (messagesToSchedule.Count == 0)
        {
            return await messageStorage.AppendToStream(streamName, expectedVersion, messagesToAppend, cancellationToken);
        }

        if (messagesToAppend.Count == 0)
        {
            await messageScheduler.ScheduleMessages(streamName, messagesToSchedule, cancellationToken);

            return new AppendResult(-1);
        }

        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        await messageScheduler.ScheduleMessages(streamName, messagesToSchedule, cancellationToken);

        var result = await messageStorage.AppendToStream(
            streamName,
            expectedVersion,
            messagesToAppend,
            cancellationToken
        );

        transactionScope.Complete();

        return result;
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken)
    {
        return messageStorage.ReadStream(streamName, options, cancellationToken);
    }
}
