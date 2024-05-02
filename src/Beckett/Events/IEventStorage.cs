namespace Beckett.Events;

public interface IEventStorage
{
    Task Initialize(CancellationToken cancellationToken);

    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}
