namespace Beckett.MessageStorage;

public interface IMessageStorage
{
    Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken
    );

    Task<ReadGlobalStreamResult> ReadGlobalStream(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );

    Task<MessageStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions options,
        CancellationToken cancellationToken
    );
}
