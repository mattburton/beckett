using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Storage.Postgres.Types;
using Beckett.Subscriptions;

namespace Beckett.Storage.Postgres;

public class PostgresSubscriptionStorage(
    BeckettOptions beckett,
    IPostgresDatabase database,
    IPostgresNotificationListener listener
) : ISubscriptionStorage
{
    public async Task AddOrUpdateSubscription(string subscriptionName, string[] eventTypes, bool startFromBeginning,
        CancellationToken cancellationToken)
    {
        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await AddOrUpdateSubscriptionQuery.Execute(
            connection,
            beckett.Postgres.Schema,
            subscriptionName,
            eventTypes,
            startFromBeginning,
            cancellationToken
        );
    }

    public IEnumerable<Task> ConfigureBackgroundService(ISubscriptionProcessor processor, CancellationToken stoppingToken)
    {
        if (beckett.Postgres.EnableNotifications)
        {
            yield return listener.Listen(
                "beckett:poll",
                (_, _) => processor.Poll(stoppingToken),
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
            beckett.Postgres.Schema,
            batchSize,
            cancellationToken
        );
    }

    public async Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        long? fromStreamPosition,
        int batchSize,
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
                    beckett.Postgres.Schema,
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
                    beckett.Postgres.Schema,
                    subscriptionStream.StreamName,
                    new ReadOptions
                    {
                        StartingStreamPosition = fromStreamPosition.Value,
                        Count = batchSize
                    },
                    cancellationToken
                );
            }

            var events = new List<EventData>();

            foreach (var subscriptionStreamEvent in subscriptionStreamEvents)
            {
                var (type, data, metadata) = PostgresEventDeserializer.DeserializeAll(subscriptionStreamEvent);

                events.Add(new EventData(
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

            if (fromStreamPosition.HasValue && result is ProcessSubscriptionStreamResult.Blocked blockedAtPosition)
            {
                throw blockedAtPosition.Exception;
            }

            switch (result)
            {
                case ProcessSubscriptionStreamResult.Success success:
                    await RecordCheckpointQuery.Execute(
                        connection,
                        beckett.Postgres.Schema,
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
                        beckett.Postgres.Schema,
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
