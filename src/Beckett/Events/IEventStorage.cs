namespace Beckett.Events;

public interface IEventStorage
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<EventEnvelope> events,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}
