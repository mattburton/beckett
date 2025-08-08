using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Storage;

namespace Beckett;

public interface IMessageStore
{
    /// <summary>
    /// Append a message to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    );

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
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Read a message stream, returning an entire message stream in order from beginning to end
    /// </summary>
    /// <param name="streamName">The name of the stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<IMessageStream> ReadStream(
        string streamName,
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

public class MessageStore(
    IMessageStorage messageStorage,
    IInstrumentation instrumentation
) : IMessageStore
{
    public Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    ) => AppendToStream(streamName, expectedVersion, [message], cancellationToken);

    public async Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    )
    {
        var activityMetadata = new Dictionary<string, string>();

        using var activity = instrumentation.StartAppendToStreamActivity(streamName, activityMetadata);

        var messagesToAppend = new List<Message>();

        foreach (var message in messages)
        {
            if (message is not Message envelope)
            {
                envelope = new Message(message);
            }

            envelope.Metadata.Prepend(activityMetadata);

            messagesToAppend.Add(envelope);
        }

        var result = await messageStorage.AppendToStream(
            streamName,
            expectedVersion,
            messagesToAppend,
            cancellationToken
        );

        return new AppendResult(result.StreamVersion);
    }

    public Task<IMessageStream> ReadStream(string streamName, CancellationToken cancellationToken) =>
        ReadStream(streamName, ReadOptions.Default, cancellationToken);

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
