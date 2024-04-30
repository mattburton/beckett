using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Events;

namespace Beckett;

public class EventStore(BeckettOptions beckettOptions, IDataSource dataSource) : IEventStore
{
    public async Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> events,
        CancellationToken cancellationToken
    )
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        //TODO - populate metadata from tracing
        var metadata = new Dictionary<string, object>();

        var newStreamEvents = events.Select(x => NewStreamEvent.From(x, metadata)).ToArray();

        var streamVersion = await AppendToStreamQuery.Execute(
            connection,
            streamName,
            expectedVersion.Value,
            newStreamEvents,
            beckettOptions.Subscriptions.UseNotifications,
            cancellationToken
        );

        return new AppendResult(streamVersion);
    }

    public async Task<IReadResult> ReadStream(string streamName, ReadOptions options,
        CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var streamEvents = await ReadStreamQuery.Execute(
            connection,
            streamName,
            options,
            cancellationToken
        );

        //TODO update query to always return actual stream version regardless of read options supplied
        var streamVersion = streamEvents.Count == 0 ? 0 : streamEvents[^1].StreamPosition;

        var events = streamEvents.Select(EventSerializer.Deserialize).ToList();

        return new ReadResult(events, streamVersion);
    }
}
