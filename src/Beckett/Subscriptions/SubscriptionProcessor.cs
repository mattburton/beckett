using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Beckett.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions;

public class SubscriptionProcessor(
    BeckettOptions beckett,
    ISubscriptionStorage subscriptionStorage,
    IServiceProvider serviceProvider,
    ILogger<SubscriptionProcessor> logger
) : ISubscriptionProcessor
{
    private BufferBlock<SubscriptionStream> _queue = null!;
    private ActionBlock<SubscriptionStream> _consumer = null!;
    private Task _poller = Task.CompletedTask;
    private bool _pendingEvents;

    public void Initialize(CancellationToken stoppingToken)
    {
        _queue = new BufferBlock<SubscriptionStream>(
            new DataflowBlockOptions
            {
                BoundedCapacity = beckett.Subscriptions.BufferSize,
                EnsureOrdered = true
            }
        );

        var concurrency = Debugger.IsAttached ? 1 : beckett.Subscriptions.Concurrency;

        _consumer = new ActionBlock<SubscriptionStream>(
            subscriptionStream => ProcessSubscriptionStream(subscriptionStream, stoppingToken),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = concurrency,
                BoundedCapacity = concurrency * 4,
                EnsureOrdered = true,
                SingleProducerConstrained = true,
                CancellationToken = stoppingToken
            }
        );

        _queue.LinkTo(
            _consumer,
            new DataflowLinkOptions
            {
                PropagateCompletion = true
            }
        );
    }

    public void Poll(CancellationToken cancellationToken)
    {
        if (!_poller.IsCompleted)
        {
            _pendingEvents = true;

            return;
        }

        _poller = PollWhileEventsAvailable(cancellationToken);
    }

    public async Task ProcessSubscriptionStreamAtPosition(
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    )
    {
        var subscription = SubscriptionRegistry.GetSubscription(subscriptionName);

        if (subscription.Handler == null)
        {
            throw new Exception($"Unknown subscription: {subscriptionName}");
        }

        var subscriptionStream = new SubscriptionStream(subscriptionName, streamName);

        await subscriptionStorage.ProcessSubscriptionStream(
            subscription,
            subscriptionStream,
            streamPosition,
            1,
            ProcessSubscriptionStreamCallback,
            cancellationToken
        );
    }

    private async Task PollWhileEventsAvailable(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var subscriptionStreams = await subscriptionStorage.GetSubscriptionStreamsToProcess(
                    beckett.Subscriptions.BatchSize,
                    cancellationToken
                );

                if (subscriptionStreams.Count == 0)
                {
                    if (_pendingEvents)
                    {
                        _pendingEvents = false;

                        continue;
                    }

                    break;
                }

                foreach (var subscriptionStream in subscriptionStreams)
                {
                    subscriptionStream.EnsureSubscriptionTypeIsValid();

                    await _consumer.SendAsync(subscriptionStream, cancellationToken);
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (NpgsqlException e)
            {
                logger.LogError(e, "Database error - will retry in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled exception reading subscription streams");
            }
        }
    }

    private async Task ProcessSubscriptionStream(
        SubscriptionStream subscriptionStream,
        CancellationToken cancellationToken
    )
    {
        var subscription = SubscriptionRegistry.GetSubscription(subscriptionStream.SubscriptionName);

        if (subscription.Handler == null)
        {
            return;
        }

        await subscriptionStorage.ProcessSubscriptionStream(
            subscription,
            subscriptionStream,
            null,
            beckett.Subscriptions.BatchSize,
            ProcessSubscriptionStreamCallback,
            cancellationToken
        );
    }

    private async Task<ProcessSubscriptionStreamResult> ProcessSubscriptionStreamCallback(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        IReadOnlyList<EventData> events,
        CancellationToken cancellationToken)
    {
        try
        {
            if (events.Count == 0)
            {
                return new ProcessSubscriptionStreamResult.NoEvents();
            }

            foreach (var @event in events)
            {
                if (!subscription.SubscribedToEvent(@event.Type))
                {
                    continue;
                }

                using var scope = serviceProvider.CreateScope();

                var context = @event.WithServices(scope);

                //TODO - support handlers that accept the event context as an argument
                var handler = scope.ServiceProvider.GetRequiredService(subscription.Type);

                try
                {
                    await subscription.Handler!(handler, context.Data, cancellationToken);
                }
                catch(Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching event {EventType} to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [ID: {EventId}]",
                        @event.Type,
                        subscriptionStream.SubscriptionName,
                        subscriptionStream.StreamName,
                        subscription.Type,
                        @event.Id
                    );

                    return new ProcessSubscriptionStreamResult.Blocked(@event.StreamPosition, e);
                }
            }

            return new ProcessSubscriptionStreamResult.Success(events[^1].StreamPosition);
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception processing subscription stream");

            return new ProcessSubscriptionStreamResult.Error();
        }
    }
}
