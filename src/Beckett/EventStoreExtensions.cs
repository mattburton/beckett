namespace Beckett;

public static class EventStoreExtensions
{
    public static Task<IAppendResult> AppendToStream(
        this IEventStore eventStore,
        string streamName,
        ExpectedVersion expectedVersion,
        object @event,
        CancellationToken cancellationToken
    )
    {
        return eventStore.AppendToStream(streamName, expectedVersion, [@event], cancellationToken);
    }

    public static Task<IReadResult> ReadStream(
        this IEventStore eventStore,
        string streamName,
        CancellationToken cancellationToken
    )
    {
        return eventStore.ReadStream(streamName, ReadOptions.Default, cancellationToken);
    }
}
