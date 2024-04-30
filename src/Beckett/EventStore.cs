using Beckett.Storage;

namespace Beckett;

public class EventStore(IStorageProvider storageProvider) : IEventStore
{
    public Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    )
    {
        return storageProvider.AppendToStream(streamName, expectedVersion, events, cancellationToken);
    }

    public Task<IReadResult> ReadStream(string streamName, ReadOptions options,
        CancellationToken cancellationToken)
    {
        return storageProvider.ReadStream(streamName, options, cancellationToken);
    }
}
