namespace Beckett.Storage;

public interface IMessageStorage
{
    Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken
    );

    Task<ReadGlobalStreamResult> ReadGlobalStream(
        ReadGlobalStreamOptions options,
        CancellationToken cancellationToken
    );

    Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions options,
        CancellationToken cancellationToken
    );
}
