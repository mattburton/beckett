using Beckett.Messages;
using Beckett.OpenTelemetry;

namespace Beckett;

public readonly struct MessageStreamSession(
    string streamName,
    ExpectedVersion expectedVersion,
    IInstrumentation instrumentation
)
{
    internal string StreamName => streamName;
    public ExpectedVersion ExpectedVersion => expectedVersion;
    internal List<MessageEnvelope> Messages { get; } = [];

    public void Append(object message) => Append([message]);

    public void Append(IEnumerable<object> messages)
    {
        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartSessionAppendToStreamActivity(streamName, metadata);

        foreach (var message in messages)
        {
            var messageToAppend = message;

            var messageMetadata = new Dictionary<string, object>(metadata);

            if (message is MessageMetadataWrapper messageWithMetadata)
            {
                foreach (var item in messageWithMetadata.Metadata)
                {
                    messageMetadata.TryAdd(item.Key, item.Value);
                }

                messageToAppend = messageWithMetadata.Message;
            }

            Messages.Add(new MessageEnvelope(messageToAppend, messageMetadata));
        }
    }
}
