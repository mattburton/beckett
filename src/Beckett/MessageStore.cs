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
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var activityMetadata = new Dictionary<string, string>();

        using var activity = instrumentation.StartAppendToStreamActivity(streamName, activityMetadata);

        var messagesToAppend = messages.ToList();

        foreach (var message in messagesToAppend)
        {
            message.Metadata.Prepend(activityMetadata);
        }

        var result = await messageStorage.AppendToStream(
            streamName,
            expectedVersion,
            messagesToAppend,
            cancellationToken
        );

        return new AppendResult(result.StreamVersion);
    }

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

