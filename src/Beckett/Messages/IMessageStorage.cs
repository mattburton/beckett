namespace Beckett.Messages;

public interface IMessageStorage
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        AppendToStreamDelegate appendToStream,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<StreamChange>> ReadStreamChangeFeed(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );
}
