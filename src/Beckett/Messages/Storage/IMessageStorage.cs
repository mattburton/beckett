namespace Beckett.Messages.Storage;

public interface IMessageStorage
{
    Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    );

    Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions options,
        CancellationToken cancellationToken
    );

    Task<ReadGlobalStreamResult> ReadGlobalStream(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );
}
