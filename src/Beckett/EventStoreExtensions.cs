namespace Beckett;

public static class EventStoreExtensions
{
    public static Task<AppendResult> AppendToStream(
        this IEventStore eventStore,
        string streamName,
        ExpectedVersion expectedVersion,
        object @event,
        CancellationToken cancellationToken
    )
    {
        return eventStore.AppendToStream(streamName, expectedVersion, [@event], cancellationToken);
    }

    public static Task<ReadResult> ReadStream(
        this IEventStore eventStore,
        string streamName,
        CancellationToken cancellationToken
    )
    {
        return eventStore.ReadStream(streamName, ReadOptions.Default, cancellationToken);
    }
}
