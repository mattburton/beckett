using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.OpenTelemetry;

namespace Beckett;

public class MessageStore(
    IMessageStorage messageStorage,
    IInstrumentation instrumentation
) : IMessageStore
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    )
    {
        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartAppendToStreamActivity(streamName, metadata);

        var messagesToAppend = new List<MessageEnvelope>();

        foreach (var message in messages)
        {
            var messageToAppend = message;

            var messageMetadata = new Dictionary<string, object>(metadata);

            if (message is MessageMetadataWrapper messageWithMetadata)
            {
                foreach (var item in messageWithMetadata.Metadata) messageMetadata.TryAdd(item.Key, item.Value);

                messageToAppend = messageWithMetadata.Message;
            }

            messagesToAppend.Add(new MessageEnvelope(messageToAppend, messageMetadata));
        }

        var result = await messageStorage.AppendToStream(
            streamName,
            expectedVersion,
            messagesToAppend,
            cancellationToken
        );

        return new AppendResult(result.StreamVersion);
    }

    public async Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        using var activity = instrumentation.StartReadStreamActivity(streamName);

        var result = await messageStorage.ReadStream(streamName, ReadStreamOptions.From(options), cancellationToken);

        return new ReadResult(
            result.StreamName,
            result.StreamVersion,
            result.Messages.Select(x => x.Message).ToList(),
            AppendToStream
        );
    }
}
