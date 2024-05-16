namespace Beckett.Messages;

public interface IMessageStorage
{
    Task<AppendResult> AppendToStream(
        string topic,
        string streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string topic,
        string streamId,
        ReadOptions options,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<StreamChange>> ReadStreamChanges(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );
}
