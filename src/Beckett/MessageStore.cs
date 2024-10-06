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
    ) => InternalAppendToStream(streamName, expectedVersion, [new Message(message)], cancellationToken);

    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        Message message,
        CancellationToken cancellationToken
    ) => InternalAppendToStream(streamName, expectedVersion, [message], cancellationToken);

    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => InternalAppendToStream(streamName, expectedVersion, messages.Select(x => new Message(x)), cancellationToken);

    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    ) => InternalAppendToStream(streamName, expectedVersion, messages, cancellationToken);

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

    private async Task<AppendResult> InternalAppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var activityMetadata = new Dictionary<string, object>();

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
}

