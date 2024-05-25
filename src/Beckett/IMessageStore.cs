namespace Beckett;

public interface IMessageStore
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    ) => AppendToStream(streamName, expectedVersion, [message], cancellationToken);

    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string streamName,
        CancellationToken cancellationToken
    ) => ReadStream(streamName, ReadOptions.Default, cancellationToken);

    Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}
