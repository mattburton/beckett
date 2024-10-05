using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.OpenTelemetry;

namespace Beckett;

public class MessageStore(
    IMessageStorage messageStorage,
    IInstrumentation instrumentation
) : IMessageStore
{
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
            string? typeName = null;
            var messageToAppend = message;
            var metadataForMessage = new Dictionary<string, object>(metadata);

            switch (message)
            {
                case MessageMetadataWrapper messageWithMetadata:
                {
                    foreach (var item in messageWithMetadata.Metadata)
                    {
                        metadataForMessage.TryAdd(item.Key, item.Value);
                    }

                    typeName = MessageTypeMap.GetName(messageWithMetadata.Message.GetType());
                    messageToAppend = messageWithMetadata.Message;
                    break;
                }
                case Message genericMessage:
                {
                    foreach (var item in genericMessage.Metadata)
                    {
                        metadataForMessage.TryAdd(item.Key, item.Value);
                    }

                    typeName = genericMessage.Type;
                    messageToAppend = genericMessage.Data;
                    break;
                }
            }

            var type = typeName ?? MessageTypeMap.GetName(message.GetType());
            var data = StaticMessageSerializer.Serialize(messageToAppend);

            messagesToAppend.Add(new MessageEnvelope(type, data, metadataForMessage));
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

