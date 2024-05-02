using Beckett.Events;
using Beckett.Storage.Postgres.Queries;
using Beckett.Subscriptions;
using Npgsql;

namespace Beckett.Storage.Postgres;

internal class PostgresSubscriptionStorage(
    BeckettOptions beckett,
    IPostgresDatabase database,
    IPostgresNotificationListener listener
) : ISubscriptionStorage
{
    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (!beckett.Postgres.RunMigrationsAtStartup)
        {
            return;
        }

        await using var connection = beckett.Postgres.MigrationConnectionString == null
            ? database.CreateConnection()
            : new NpgsqlConnection(beckett.Postgres.MigrationConnectionString);

        await connection.OpenAsync(cancellationToken);

        await PostgresMigrator.Execute(
            connection,
            beckett.Postgres.Schema,
            beckett.Postgres.MigrationAdvisoryLockId,
            cancellationToken
        );
    }

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

    public IEnumerable<Task> ConfigureServiceHost(ISubscriptionProcessor processor, CancellationToken stoppingToken)
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
                beckett.Postgres.Schema,
                subscriptionStream.SubscriptionName,
                subscriptionStream.StreamName,
                batchSize,
                cancellationToken
            );

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
