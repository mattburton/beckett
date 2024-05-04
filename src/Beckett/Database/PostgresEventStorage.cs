using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Events;

namespace Beckett.Database;

public class PostgresEventStorage(
    IEventSerializer eventSerializer,
    IPostgresDatabase database,
    IPostgresEventDeserializer eventDeserializer
) : IEventStorage
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<EventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        var newEvents = events.Select(x => EventType.From(x.Event, x.Metadata, eventSerializer)).ToArray();

        var streamVersion = await database.Execute(
            new AppendToStream(streamName, expectedVersion.Value, newEvents),
            cancellationToken
        );

        return new AppendResult(streamVersion);
    }

    public async Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var streamEvents = await database.Execute(new ReadStream(streamName, readOptions), cancellationToken);

        //TODO update query to always return actual stream version regardless of read options supplied
        var streamVersion = streamEvents.Count == 0 ? 0 : streamEvents[^1].StreamPosition;

        var events = streamEvents.Select(eventDeserializer.Deserialize).ToList();

        return new ReadResult(events, streamVersion);
    }

    public async Task<IReadOnlyList<StreamChange>> ReadStreamChanges(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(
            new ReadStreamChanges(lastGlobalPosition, batchSize),
            cancellationToken
        );

        return results.Count == 0
            ? []
            : results.Select(x => new StreamChange(
                x.StreamName,
                x.StreamVersion,
                x.GlobalPosition,
                x.EventTypes
            )).ToList();
    }
}
