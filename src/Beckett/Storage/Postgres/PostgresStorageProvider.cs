using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;
using Beckett.Subscriptions;

namespace Beckett.Storage.Postgres;

public class PostgresStorageProvider(
    BeckettOptions beckettOptions,
    IPostgresDatabase database,
    IPostgresNotificationListener listener
) : IStorageProvider
{
    public async Task AddOrUpdateSubscription(string subscriptionName, string[] eventTypes, bool startFromBeginning,
        CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await AddOrUpdateSubscriptionQuery.Execute(
            connection,
            subscriptionName,
            eventTypes,
            startFromBeginning,
            cancellationToken
        );
    }

    public async Task<IAppendResult> AppendToStream(string streamName, ExpectedVersion expectedVersion,
        IEnumerable<object> events, CancellationToken cancellationToken)
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

    public IEnumerable<Task> GetSubscriptionHostTasks(ISubscriptionStreamProcessor processor, CancellationToken stoppingToken)
    {
        if (beckettOptions.Postgres.EnableNotifications)
        {
            yield return listener.Listen(
                "beckett:poll",
                (_, _) => processor.StartPolling(stoppingToken),
                stoppingToken
            );
        }
    }

    public async Task<IReadOnlyList<SubscriptionStream>> GetSubscriptionStreamsToProcess(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        return await GetSubscriptionStreamsToProcessQuery.Execute(
            connection,
            beckettOptions.Subscriptions.BatchSize,
            cancellationToken
        );
    }

    public async Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        ProcessSubscriptionStreamCallback callback,
        CancellationToken cancellationToken
    )
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var advisoryLockId = subscriptionStream.ToAdvisoryLockId();

        var locked = await connection.TryAdvisoryLock(advisoryLockId, cancellationToken);

        if (!locked)
        {
            return;
        }

        try
        {
            var subscriptionStreamEvents = await ReadSubscriptionStreamQuery.Execute(
                connection,
                subscriptionStream.SubscriptionName,
                subscriptionStream.StreamName,
                beckettOptions.Subscriptions.BatchSize,
                cancellationToken
            );

            var events = new List<EventContext>();

            foreach (var subscriptionStreamEvent in subscriptionStreamEvents)
            {
                var (type, data, metadata) = EventSerializer.DeserializeAll(subscriptionStreamEvent);

                events.Add(new EventContext(
                    subscriptionStreamEvent.Id,
                    subscriptionStreamEvent.StreamName,
                    subscriptionStreamEvent.StreamPosition,
                    subscriptionStreamEvent.GlobalPosition,
                    type,
                    data,
                    metadata,
                    subscriptionStreamEvent.Timestamp
                ));
            }

            var result = await callback(subscription, subscriptionStream, events, cancellationToken);

            switch (result)
            {
                case ProcessSubscriptionStreamResult.Success success:
                    await RecordCheckpointQuery.Execute(
                        connection,
                        subscriptionStream.SubscriptionName,
                        subscriptionStream.StreamName,
                        success.StreamPosition,
                        false,
                        cancellationToken
                    );
                    break;
                case ProcessSubscriptionStreamResult.Blocked blocked:
                    await RecordCheckpointQuery.Execute(
                        connection,
                        subscriptionStream.SubscriptionName,
                        subscriptionStream.StreamName,
                        blocked.StreamPosition,
                        true,
                        cancellationToken
                    );
                    break;
            }
        }
        finally
        {
            await connection.AdvisoryUnlock(advisoryLockId, cancellationToken);
        }
    }

    public async Task<IReadResult> ReadStream(string streamName, ReadOptions options,
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

        var events = streamEvents.Select(EventSerializer.Deserialize).ToList();

        return new ReadResult(events, streamVersion);
    }

    public Task<IReadResult> ReadSubscriptionStream(SubscriptionStream subscriptionStream, int batchSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RecordCheckpoint(SubscriptionStream subscriptionStream, long checkpoint, bool blocked,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
