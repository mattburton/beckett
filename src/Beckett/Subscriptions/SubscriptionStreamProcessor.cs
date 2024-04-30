using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Beckett.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class SubscriptionStreamProcessor(
    BeckettOptions options,
    IStorageProvider storageProvider,
    IServiceProvider serviceProvider,
    ILogger<SubscriptionStreamProcessor> logger
) : ISubscriptionStreamProcessor
{
    private BufferBlock<SubscriptionStream> _workQueue = null!;
    private ActionBlock<SubscriptionStream> _worker = null!;
    private Task _poller = Task.CompletedTask;
    private bool _pendingEvents;

    public void Initialize(CancellationToken stoppingToken)
    {
        _workQueue = new BufferBlock<SubscriptionStream>(
            new DataflowBlockOptions
            {
                BoundedCapacity = options.Subscriptions.BufferSize,
                EnsureOrdered = true
            }
        );

        var concurrency = Debugger.IsAttached ? 1 : options.Subscriptions.Concurrency;

        _worker = new ActionBlock<SubscriptionStream>(
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

        _workQueue.LinkTo(
            _worker,
            new DataflowLinkOptions
            {
                PropagateCompletion = true
            }
        );
    }

    public void StartPolling(CancellationToken cancellationToken)
    {
        if (!_poller.IsCompleted)
        {
            _pendingEvents = true;

            return;
        }

        _poller = Poll(cancellationToken);
    }

    public async Task Poll(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var subscriptionStreams = await storageProvider.GetSubscriptionStreamsToProcess(
                    options.Subscriptions.BatchSize,
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

                    await _worker.SendAsync(subscriptionStream, cancellationToken);
                }
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

        await storageProvider.ProcessSubscriptionStream(
            subscription,
            subscriptionStream,
            ProcessSubscriptionStreamCallback,
            cancellationToken
        );
    }

    private async Task<ProcessSubscriptionStreamResult> ProcessSubscriptionStreamCallback(
        Subscription subscription,
        SubscriptionStream subscriptionStream,
        IReadOnlyList<EventContext> events,
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

                var handler = scope.ServiceProvider.GetRequiredService(subscription.Type);

                try
                {
                    await subscription.Handler!(handler, @event.Data, cancellationToken);
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

                    return new ProcessSubscriptionStreamResult.Blocked(@event.StreamPosition);
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
