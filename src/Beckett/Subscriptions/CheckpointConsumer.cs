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
    private Task _task = Task.CompletedTask;
    private int _continue;

    public void StartPolling()
    {
        if (_task is { IsCompleted: false })
        {
            logger.NewCheckpointsAvailableWhileConsumerIsActive(instance);

            Interlocked.Exchange(ref _continue, 1);

            return;
        }

        _task = Poll();
    }

    private async Task Poll()
    {
        while (true)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

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
                    if (Interlocked.CompareExchange(ref _continue, 0, 1) == 1)
                    {
                        logger.WillContinuePollingCheckpoints(instance);

                        continue;
                    }

                    logger.NoNewCheckpointsFoundExiting(instance);

                    break;
                }

                if (!checkpoint.IsRetryOrFailure && checkpoint.StreamPosition >= checkpoint.StreamVersion)
                {
                    logger.SkippingCheckpointAlreadyCaughtUp(checkpoint.Id, checkpoint.StreamPosition, instance);

                    await database.Execute(
                        new ReleaseCheckpointReservation(checkpoint.Id, options.Postgres),
                        stoppingToken
                    );

                    continue;
                }

                var subscription = SubscriptionRegistry.GetSubscription(checkpoint.Name);

                if (subscription == null)
                {
                    logger.SubscriptionNotRegistered(checkpoint.Name, options.Subscriptions.GroupName, checkpoint.Id, instance);

                    continue;
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
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
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing checkpoint [Consumer: {Consumer}]", instance);
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

    [LoggerMessage(0, LogLevel.Trace, "Skipping checkpoint {CheckpointId} - already caught up at stream position {StreamPosition} - releasing reservation and continuing polling in consumer {Consumer}")]
    public static partial void SkippingCheckpointAlreadyCaughtUp(this ILogger logger, long checkpointId, long streamPosition, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "Subscription {SubscriptionName} not registered for group {GroupName} - skipping checkpoint {CheckpointId} in consumer {Consumer}")]
    public static partial void SubscriptionNotRegistered(this ILogger logger, string subscriptionName, string groupName, long checkpointId, int consumer);
}
