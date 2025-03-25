using System.Threading.Channels;
using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumer(
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    BeckettOptions options,
    ILogger<CheckpointConsumer> logger
)
{
    public async Task Poll(int instance, Channel<CheckpointAvailable> channel, CancellationToken stoppingToken)
    {
        logger.StartingCheckpointPolling(instance);

        await foreach (var _ in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                logger.AttemptingToReserveCheckpoint(instance);

                //once we reserve a checkpoint the stopping token is ignored so the checkpoint can be processed prior
                //to shutting down the host - reservation timeout applies
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
                    logger.NoAvailableCheckpoints(instance);

                    continue;
                }

                if (!checkpoint.IsRetryOrFailure && checkpoint.StreamPosition >= checkpoint.StreamVersion)
                {
                    logger.CheckpointAlreadyCaughtUp(checkpoint.Id, checkpoint.StreamPosition, instance);

                    await database.Execute(
                        new ReleaseCheckpointReservation(checkpoint.Id, options.Postgres),
                        CancellationToken.None
                    );

                    continue;
                }

                var subscription = SubscriptionRegistry.GetSubscription(checkpoint.Name);

                if (subscription == null)
                {
                    logger.SubscriptionNotRegistered(
                        checkpoint.Name,
                        options.Subscriptions.GroupName,
                        checkpoint.Id,
                        instance
                    );

                    continue;
                }

                await checkpointProcessor.Process(instance, checkpoint, subscription);

                channel.Writer.TryWrite(CheckpointAvailable.Instance);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
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
    [LoggerMessage(0, LogLevel.Trace, "Starting checkpoint polling for consumer {Consumer}")]
    public static partial void StartingCheckpointPolling(this ILogger logger, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "Attempting to reserve checkpoint for consumer {Consumer}")]
    public static partial void AttemptingToReserveCheckpoint(this ILogger logger, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "No available checkpoints - will continue to wait for consumer {Consumer}")]
    public static partial void NoAvailableCheckpoints(this ILogger logger, int consumer);

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Skipping checkpoint {CheckpointId} - already caught up at stream position {StreamPosition} - releasing reservation and continuing polling in consumer {Consumer}"
    )]
    public static partial void CheckpointAlreadyCaughtUp(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        int consumer
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Subscription {SubscriptionName} not registered for group {GroupName} - skipping checkpoint {CheckpointId} in consumer {Consumer}"
    )]
    public static partial void SubscriptionNotRegistered(
        this ILogger logger,
        string subscriptionName,
        string groupName,
        long checkpointId,
        int consumer
    );
}
