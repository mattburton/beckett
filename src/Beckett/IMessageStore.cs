namespace Beckett;

public interface IMessageStore
{
    IAdvancedOperations Advanced { get; }

    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    );

    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<MessageStream> ReadStream(
        string streamName,
        CancellationToken cancellationToken
    );

    Task<MessageStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}

public interface IAdvancedOperations
{
    IMessageStoreSession CreateSession();

    IMessageStreamBatch ReadStreamBatch();
}

public interface IMessageStoreSession
{
    MessageStreamSession AppendToStream(string streamName, ExpectedVersion expectedVersion);

    Task SaveChanges(CancellationToken cancellationToken);
}

public interface IMessageStreamBatch
{
    Task<MessageStream> ReadStream(string streamName, ReadOptions? readOptions = null);

    Task Execute(CancellationToken cancellationToken);
}
