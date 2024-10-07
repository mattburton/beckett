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
        var activityMetadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartAppendToStreamActivity(streamName, activityMetadata);

        var messagesToAppend = new List<Message>();

        foreach (var message in messages)
        {
            if (message is not Message messageToAppend)
            {
                messageToAppend = new Message(message, activityMetadata);
            }
            else
            {
                messageToAppend.Metadata.Prepend(activityMetadata);
            }

            messagesToAppend.Add(messageToAppend);
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

