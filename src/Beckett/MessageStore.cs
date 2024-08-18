using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.OpenTelemetry;

namespace Beckett;

public class MessageStore(
    IMessageStorage messageStorage,
    IInstrumentation instrumentation
) : IMessageStore
{
    public IAdvancedOperations Advanced => new AdvancedOperations(messageStorage, AppendToStream);

    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    ) => AppendToStream(streamName, expectedVersion, [message], cancellationToken);

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

    public Task<MessageStream> ReadStream(string streamName, CancellationToken cancellationToken) =>
        ReadStream(streamName, ReadOptions.Default, cancellationToken);

    public async Task<MessageStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        using var activity = instrumentation.StartReadStreamActivity(streamName);

        var result = await messageStorage.ReadStream(streamName, ReadStreamOptions.From(options), cancellationToken);

        return new MessageStream(
            result.StreamName,
            result.StreamVersion,
            result.Messages,
            AppendToStream
        );
    }
}

public class AdvancedOperations(
    IMessageStorage messageStorage,
    AppendToStreamDelegate appendToStream
) : IAdvancedOperations
{
    public IMessageStoreSession CreateSession() => messageStorage.CreateSession();

    public IMessageStreamBatch ReadStreamBatch() => messageStorage.CreateStreamBatch(appendToStream);
}

