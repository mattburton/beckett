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

    Task<IReadOnlyList<StreamChange>> ReadStreamChanges(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );
}

public record StreamChange(string StreamName, long StreamVersion, long GlobalPosition, string[] EventTypes);
