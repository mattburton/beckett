using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumer(
    int instance,
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    BeckettOptions options,
    ILogger<CheckpointConsumer> logger,
    CancellationToken stoppingToken
) : ICheckpointConsumer
{
    private readonly object _lock = new();
    private Task _task = Task.CompletedTask;
    private bool _continue;

    public void StartPolling()
    {
        if (_task is { IsCompleted: false })
        {
            lock (_lock)
            {
                logger.NewCheckpointsAvailableWhileConsumerIsActive(instance);

                _continue = true;
            }

            return;
        }

        _task = Poll();
    }

    private async Task Poll()
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.StartingCheckpointPolling(instance);

                var checkpoint = await database.Execute(
                    new ReserveNextAvailableCheckpoint(
                        options.Subscriptions.GroupName,
                        options.Subscriptions.ReservationTimeout,
                        options.Postgres
                    ),
                    stoppingToken
                );

                if (checkpoint == null)
                {
                    if (_continue)
                    {
                        lock (_lock)
                        {
                            if (_continue)
                            {
                                logger.WillContinuePollingCheckpoints(instance);

                                _continue = false;

                                continue;
                            }
                        }
                    }

                    logger.NoNewCheckpointsFoundExiting(instance);

                    break;
                }

                var subscription = SubscriptionRegistry.GetSubscription(checkpoint.Name);

                if (subscription == null)
                {
                    logger.SubscriptionNotRegistered(checkpoint.Name, options.Subscriptions.GroupName, checkpoint.Id, instance);

                    continue;
                }

                await checkpointProcessor.Process(
                    instance,
                    checkpoint,
                    subscription,
                    stoppingToken
                );
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing checkpoint [Consumer: {Consumer}]", instance);

                throw;
            }
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "New checkpoints are available but consumer is already active - setting continue flag to true for consumer {Consumer}")]
    public static partial void NewCheckpointsAvailableWhileConsumerIsActive(this ILogger logger, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "Starting checkpoint polling for consumer {Consumer}")]
    public static partial void StartingCheckpointPolling(this ILogger logger, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "No new checkpoints were found but will continue polling since the continue flag has been set to true for consumer {Consumer}")]
    public static partial void WillContinuePollingCheckpoints(this ILogger logger, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "No new checkpoints found - exiting consumer {Consumer}")]
    public static partial void NoNewCheckpointsFoundExiting(this ILogger logger, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "Subscription {SubscriptionName} not registered for group {GroupName} - skipping checkpoint {CheckpointId} in consumer {Consumer}")]
    public static partial void SubscriptionNotRegistered(this ILogger logger, string subscriptionName, string groupName, long checkpointId, int consumer);
}
