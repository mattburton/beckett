using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.OpenTelemetry;

namespace Beckett;

public interface IMessageStore
{
    /// <summary>
    /// Append messages to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Read a message stream passing in options to control how the stream is read - starting / ending positions,
    /// backwards or forwards, etc...
    /// </summary>
    /// <param name="streamName">The name of the stream</param>
    /// <param name="options">The read options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<IMessageStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}

public static class MessageStoreExtensions
{
    /// <summary>
    /// Append a message to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="messageStore">The message store</param>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    public static Task<IAppendResult> AppendToStream(
        this IMessageStore messageStore,
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    ) => messageStore.AppendToStream(streamName, expectedVersion, [new Message(message)], cancellationToken);

    /// <summary>
    /// Append a message to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="messageStore">The message store</param>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    public static Task<IAppendResult> AppendToStream(
        this IMessageStore messageStore,
        string streamName,
        ExpectedVersion expectedVersion,
        Message message,
        CancellationToken cancellationToken
    ) => messageStore.AppendToStream(streamName, expectedVersion, [message], cancellationToken);

    /// <summary>
    /// Append messages to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="messageStore">The message store</param>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="messages">The messages to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    public static Task<IAppendResult> AppendToStream(
        this IMessageStore messageStore,
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => messageStore.AppendToStream(
        streamName,
        expectedVersion,
        messages.Select(x => new Message(x)),
        cancellationToken
    );

    /// <summary>
    /// Read a message stream, returning an entire message stream in order from beginning to end
    /// </summary>
    /// <param name="messageStore">The message store instance</param>
    /// <param name="streamName">The name of the stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public static Task<IMessageStream> ReadStream(
        this IMessageStore messageStore,
        string streamName,
        CancellationToken cancellationToken
    ) => messageStore.ReadStream(streamName, ReadOptions.Default, cancellationToken);
}

public class MessageStore(
    IMessageStorage messageStorage,
    IInstrumentation instrumentation
) : IMessageStore
{
    public async Task<IAppendResult> AppendToStream(
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

    public async Task<IMessageStream> ReadStream(
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
            result.StreamMessages.Select(MessageContext.From).ToList(),
            AppendToStream
        );
    }
}
