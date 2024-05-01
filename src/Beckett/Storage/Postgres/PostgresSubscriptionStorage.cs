using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Subscriptions;

namespace Beckett.Storage.Postgres;

public class PostgresSubscriptionStorage(
    BeckettOptions beckettOptions,
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
            subscriptionName,
            eventTypes,
            startFromBeginning,
            cancellationToken
        );
    }

    public IEnumerable<Task> ConfigureSubscriptionHost(ISubscriptionProcessor processor, CancellationToken stoppingToken)
    {
        if (beckettOptions.Postgres.EnableNotifications)
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
            batchSize,
            cancellationToken
        );
    }

    public async Task ProcessSubscriptionStream(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
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
            var subscriptionStreamEvents = await ReadSubscriptionStreamQuery.Execute(
                connection,
                subscriptionStream.SubscriptionName,
                subscriptionStream.StreamName,
                batchSize,
                cancellationToken
            );

            var events = new List<IEventData>();

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
}
