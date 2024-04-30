using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class SubscriptionStreamProcessor(
    BeckettOptions options,
    IDataSource dataSource,
    IServiceProvider serviceProvider,
    ILogger<SubscriptionStreamProcessor> logger
)
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
                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(cancellationToken);

                var results = await GetSubscriptionStreamsToProcessQuery.Execute(
                    connection,
                    options.Subscriptions.BatchSize,
                    cancellationToken
                );

                if (results.Count == 0)
                {
                    if (_pendingEvents)
                    {
                        _pendingEvents = false;

                        continue;
                    }

                    break;
                }

                foreach (var result in results)
                {
                    var subscriptionStream = new SubscriptionStream(
                        result.SubscriptionName,
                        result.StreamName
                    );

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

        await using var connection = dataSource.CreateConnection();

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
                options.Subscriptions.BatchSize,
                cancellationToken
            );

            long streamPosition = 0;

            foreach (var streamEvent in subscriptionStreamEvents)
            {
                streamPosition = streamEvent.StreamPosition;

                //TODO - setup tracing from metadata
                var (type, @event, _) = EventSerializer.DeserializeAll(streamEvent);

                if (!subscription.SubscribedToEvent(type))
                {
                    continue;
                }

                using var scope = serviceProvider.CreateScope();

                var handler = scope.ServiceProvider.GetRequiredService(subscription.Type);

                try
                {
                    await subscription.Handler(handler, @event, cancellationToken);
                }
                catch(Exception e)
                {
                    logger.LogError(
                        e,
                        "Error dispatching event {EventType} to subscription {SubscriptionType} for stream {StreamName} using handler {HandlerType} [ID: {EventId}]",
                        streamEvent.Type,
                        subscriptionStream.SubscriptionName,
                        subscriptionStream.StreamName,
                        subscription.Type,
                        streamEvent.Id
                    );

                    await RecordCheckpointQuery.Execute(
                        connection,
                        subscriptionStream.SubscriptionName,
                        subscriptionStream.StreamName,
                        streamPosition,
                        true,
                        cancellationToken
                    );

                    break;
                }
            }

            await RecordCheckpointQuery.Execute(
                connection,
                subscriptionStream.SubscriptionName,
                subscriptionStream.StreamName,
                streamPosition,
                false,
                cancellationToken
            );
        }
        catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception processing subscription stream");
        }
        finally
        {
            await connection.AdvisoryUnlock(advisoryLockId, cancellationToken);
        }
    }
}
