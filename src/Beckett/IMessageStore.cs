using Beckett.MessageStorage;

namespace Beckett;

public interface IMessageStore
{
    /// <summary>
    /// Advanced message store operations. If using a custom <see cref="IMessageStorage"/> implementation some
    /// advanced functionality may not be available.
    /// </summary>
    IAdvancedOperations Advanced { get; }

    /// <summary>
    /// Append a message to a stream while supplying the expected version of that stream
    /// </summary>
    /// <param name="streamName">The name of the stream to append to</param>
    /// <param name="expectedVersion">Expected version of the stream</param>
    /// <param name="message">The message to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the append operation</returns>
    Task<AppendResult> AppendToStream(
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
    Task<AppendResult> AppendToStream(
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
    Task<MessageStream> ReadStream(
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
    Task<MessageStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}

public interface IAdvancedOperations
{
    /// <summary>
    /// Create a session - unit of work - that can be used to append messages to multiple streams and write them to the
    /// message store in a single <c>SaveChanges</c> call. If using a custom <see cref="IMessageStorage"/>
    /// implementation please verify that this functionality is supported prior to use.
    /// </summary>
    /// <returns>The message store session</returns>
    IMessageStoreSession CreateSession();

    /// <summary>
    /// <para>
    /// Read multiple streams in a single round trip to the message store. Once a batch is created one or more reads
    /// can be setup, with the results available after <c>Execute</c> is called. If using a custom
    /// <see cref="IMessageStorage"/> implementation please verify that this functionality is supported prior to use.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// var batch = messageStore.ReadStreamBatch();
    /// var orderStream = batch.ReadStream("Order-1234");
    /// var customerStream = batch.ReadStream("Customer-5467");
    ///
    /// await batch.Execute(cancellationToken);
    ///
    /// var order = orderStream.Result.ProjectTo&lt;Order&gt;();
    /// var customer = customerStream.Result.ProjectTo&lt;Customer&gt;();
    /// </code>
    /// </para>
    /// </summary>
    /// <returns>The message stream batch</returns>
    IMessageStreamBatch ReadStreamBatch();
}

public interface IMessageStoreSession
{
    /// <summary>
    /// Start appending to a stream within a session, supplying the initial expected version of the stream you are
    /// appending to. The resulting message stream session instance can be appended to however many times as necessary.
    /// Once you are ready to save your changes to the stream(s) within the session just call <see cref="SaveChanges"/>
    /// to commit the unit of work.
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="expectedVersion"></param>
    /// <returns>The message stream session</returns>
    MessageStreamSession AppendToStream(string streamName, ExpectedVersion expectedVersion);

    /// <summary>
    /// Commit all outstanding appends to the message store.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveChanges(CancellationToken cancellationToken);
}

public interface IMessageStreamBatch
{
    /// <summary>
    /// Read a stream within the message stream batch, where the results are available after calling
    /// <see cref="Execute"/>.
    /// </summary>
    /// <param name="streamName">The stream name</param>
    /// <param name="readOptions">The read options (optional)</param>
    /// <returns></returns>
    Task<MessageStream> ReadStream(string streamName, ReadOptions? readOptions = null);

    /// <summary>
    /// Execute the batch, completing the pending read stream tasks with the results.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Execute(CancellationToken cancellationToken);
}
