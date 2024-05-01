using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;

namespace Beckett.Storage.Postgres;

public class PostgresEventStorage(BeckettOptions beckettOptions, IPostgresDatabase database) : IEventStorage
{
    public async Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        //TODO - populate metadata from tracing
        var metadata = new Dictionary<string, object>();

        var newStreamEvents = events.Select(x => NewStreamEvent.From(x, metadata)).ToArray();

        var streamVersion = await AppendToStreamQuery.Execute(
            connection,
            streamName,
            expectedVersion.Value,
            newStreamEvents,
            beckettOptions.Postgres.EnableNotifications,
            cancellationToken
        );

        return new AppendResult(streamVersion);
    }

    public async Task<ReadResult> ReadStream(string streamName, ReadOptions options,
        CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var streamEvents = await ReadStreamQuery.Execute(
            connection,
            streamName,
            options,
            cancellationToken
        );

        //TODO update query to always return actual stream version regardless of read options supplied
        var streamVersion = streamEvents.Count == 0 ? 0 : streamEvents[^1].StreamPosition;

        var events = streamEvents.Select(PostgresEventDeserializer.Deserialize).ToList();

        return new ReadResult(events, streamVersion);
    }
}
