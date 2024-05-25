namespace Beckett;

public interface IMessageSession
{
    void AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        object message
    ) => AppendToStream(streamName, expectedVersion, [message]);

    void AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages
    );

    Task<ReadResult> ReadStream(
        string streamName,
        CancellationToken cancellationToken
    ) => ReadStream(streamName, ReadOptions.Default, cancellationToken);

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);

    Task SaveChanges(CancellationToken cancellationToken);
}
