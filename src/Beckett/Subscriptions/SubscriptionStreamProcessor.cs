using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Events;
using Beckett.Subscriptions.Retries;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions;

public class SubscriptionStreamProcessor(
    IPostgresDatabase database,
    IPostgresEventDeserializer eventDeserializer,
    IEventStore eventStore,
    IServiceProvider serviceProvider,
    ILogger<SubscriptionStreamProcessor> logger
) : ISubscriptionStreamProcessor
{
    public async Task Process(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        bool retryOnError,
        CancellationToken cancellationToken
    )
    {
        var streamEvents = await database.Execute(
            new ReadStream(
                streamName,
                new ReadOptions
                {
                    StartingStreamPosition = streamPosition,
                    Count = batchSize
                }
            ),
            connection,
            cancellationToken
        );

        var events = new List<IEventContext>();

        foreach (var streamEvent in streamEvents)
        {
            var (type, data, metadata) = eventDeserializer.DeserializeAll(streamEvent);

            events.Add(new EventContext(
                streamEvent.Id,
                streamEvent.StreamName,
                streamEvent.StreamPosition,
                streamEvent.GlobalPosition,
                type,
                data,
                metadata,
                streamEvent.Timestamp
            ));
        }

        var result = await HandleEventBatch(subscription, streamName, events, true, cancellationToken);

        switch (result)
        {
            case EventBatchResult.Success success:
                await database.Execute(
                    new UpdateCheckpointStreamPosition(subscription.Name, streamName, success.StreamPosition, false),
                    connection,
                    transaction,
                    cancellationToken
                );

                break;
            case EventBatchResult.Blocked blocked:
                if (ShouldThrow(subscription, retryOnError))
                {
                    throw blocked.Exception;
                }

                await database.Execute(
                    new UpdateCheckpointStreamPosition(subscription.Name, streamName, blocked.StreamPosition, true),
                    connection,
                    transaction,
                    cancellationToken
                );

                break;
        }
    }

    private static bool ShouldThrow(Subscription subscription, bool retryOnError)
    {
        return !retryOnError || subscription.MaxRetryCount == 0;
    }

    private async Task<EventBatchResult> HandleEventBatch(
        Subscription subscription,
        string streamName,
        IReadOnlyList<IEventContext> events,
        bool retryOnError,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (events.Count == 0)
            {
                return new EventBatchResult.NoEvents();
            }

            foreach (var @event in events)
            {
                if (!subscription.SubscribedToEvent(@event.Type))
                {
                    continue;
                }

                using var scope = serviceProvider.CreateScope();

                //TODO - support handlers that accept the event context as an argument
                var handler = scope.ServiceProvider.GetRequiredService(subscription.Type);

                try
                {
                    await subscription.Handler!(handler, @event.Data, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching event {EventType} to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [ID: {EventId}]",
                        @event.Type,
                        subscription.Name,
                        streamName,
                        subscription.Type,
                        @event.Id
                    );

                    if (!retryOnError || subscription.MaxRetryCount == 0)
                    {
                        throw;
                    }

                    var retryAt = 0.GetNextDelayWithExponentialBackoff();

                    await eventStore.AppendToStream(
                        RetryStreamName.For(
                            subscription.Name,
                            streamName
                        ),
                        ExpectedVersion.Any,
                        new SubscriptionError(
                            subscription.Name,
                            streamName,
                            @event.StreamPosition,
                            ExceptionData.From(e),
                            retryAt,
                            DateTimeOffset.UtcNow
                        ).ScheduleAt(retryAt),
                        cancellationToken
                    );

                    return new EventBatchResult.Blocked(@event.StreamPosition, e);
                }
            }

            return new EventBatchResult.Success(events[^1].StreamPosition);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception processing subscription stream");

            throw;
        }
    }

    public abstract record EventBatchResult
    {
        public record NoEvents : EventBatchResult;

        public record Success(long StreamPosition) : EventBatchResult;

        public record Blocked(long StreamPosition, Exception Exception) : EventBatchResult;
    }
}
