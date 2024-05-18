using Beckett.Messages;
using Beckett.OpenTelemetry;

namespace Beckett;

public interface IMessageStore
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    ) => AppendToStream(streamName, expectedVersion, [message], cancellationToken);

    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string streamName,
        CancellationToken cancellationToken
    ) => ReadStream(streamName, ReadOptions.Default, cancellationToken);

    Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}

public class MessageStore(
    IMessageStorage messageStorage,
    IInstrumentation instrumentation
) : IMessageStore
{
    public Task<AppendResult> AppendToStream(
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

        return messageStorage.AppendToStream(
            streamName,
            expectedVersion,
            messagesToAppend,
            cancellationToken
        );
    }

    public Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        using var activity = instrumentation.StartReadStreamActivity(streamName);

        return messageStorage.ReadStream(streamName, options, cancellationToken);
    }
}
