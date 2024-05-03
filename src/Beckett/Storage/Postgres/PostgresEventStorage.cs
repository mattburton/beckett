using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;

namespace Beckett.Storage.Postgres;

public interface IPostgresEventStorage : IEventStorage;

public class PostgresEventStorage(
    BeckettOptions options,
    IPostgresDatabase database,
    IEventSerializer eventSerializer
) : IPostgresEventStorage
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<EventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var newStreamEvents = events.Select(x => NewEvent.From(x.Event, x.Metadata, eventSerializer)).ToArray();

        var streamVersion = await AppendToStreamQuery.Execute(
            connection,
            options.Postgres.Schema,
            streamName,
            expectedVersion.Value,
            newStreamEvents,
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
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var streamEvents = await ReadStreamQuery.Execute(
            connection,
            options.Postgres.Schema,
            streamName,
            readOptions,
            cancellationToken
        );

        //TODO update query to always return actual stream version regardless of read options supplied
        var streamVersion = streamEvents.Count == 0 ? 0 : streamEvents[^1].StreamPosition;

        var events = streamEvents.Select(x => PostgresEventDeserializer.Deserialize(x, options)).ToList();

        return new ReadResult(events, streamVersion);
    }
}
