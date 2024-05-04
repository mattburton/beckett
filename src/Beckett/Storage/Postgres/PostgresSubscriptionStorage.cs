using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;
using Beckett.Subscriptions;

namespace Beckett.Storage.Postgres;

public class PostgresSubscriptionStorage(
    BeckettOptions options,
    IPostgresDatabase database,
    IEventTypeMap eventTypeMap
) : ISubscriptionStorage
{
    public async Task AddOrUpdateSubscription(string subscriptionName, string[] eventTypes, bool startFromBeginning,
        CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await AddOrUpdateSubscriptionQuery.Execute(
            connection,
            options.Postgres.Schema,
            subscriptionName,
            eventTypes,
            startFromBeginning,
            cancellationToken
        );
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
            options.Postgres.Schema,
            batchSize,
            cancellationToken
        );
    }

    public async Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        long? fromStreamPosition,
        int batchSize,
        bool retryOnError,
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
            IReadOnlyList<StreamEvent> subscriptionStreamEvents;

            if (fromStreamPosition == null)
            {
                subscriptionStreamEvents = await ReadSubscriptionStreamQuery.Execute(
                    connection,
                    options.Postgres.Schema,
                    subscriptionStream.SubscriptionName,
                    subscriptionStream.StreamName,
                    batchSize,
                    cancellationToken
                );
            }
            else
            {
                subscriptionStreamEvents = await ReadStreamQuery.Execute(
                    connection,
                    options.Postgres.Schema,
                    subscriptionStream.StreamName,
                    new ReadOptions
                    {
                        StartingStreamPosition = fromStreamPosition.Value,
                        Count = batchSize
                    },
                    cancellationToken
                );
            }

            var events = new List<IEventContext>();

            foreach (var subscriptionStreamEvent in subscriptionStreamEvents)
            {
                var (type, data, metadata) = PostgresEventDeserializer.DeserializeAll(
                    subscriptionStreamEvent,
                    eventTypeMap
                );

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

            var result = await callback(
                subscription,
                subscriptionStream,
                events,
                retryOnError,
                cancellationToken
            );

            switch (result)
            {
                case ProcessSubscriptionStreamResult.Success success:
                    await RecordCheckpointQuery.Execute(
                        connection,
                        options.Postgres.Schema,
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
                        options.Postgres.Schema,
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
}
