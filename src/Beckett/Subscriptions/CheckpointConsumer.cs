using System.Threading.Channels;
using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumer(
    SubscriptionGroup group,
    Channel<CheckpointAvailable> channel,
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    BeckettOptions options,
    ILogger<CheckpointConsumer> logger
)
{
    public async Task Poll(int instance, CancellationToken stoppingToken)
    {
        logger.StartingCheckpointPolling(instance, group.Name);

        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channel.Reader.TryRead(out _))
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    logger.AttemptingToReserveCheckpoint(instance, group.Name);

                    //once we reserve a checkpoint the stopping token is ignored so the checkpoint can be processed prior
                    //to shutting down the host - reservation timeout applies
                    var checkpoint = await database.Execute(
                        new ReserveNextAvailableCheckpoint(
                            group.Name,
                            group.ReservationTimeout,
                            options.Subscriptions.ReplayMode
                        ),
                        stoppingToken
                    );

                    if (checkpoint == null)
                    {
                        logger.NoAvailableCheckpoints(instance, group.Name);

                        continue;
                    }

                    if (!checkpoint.IsRetryOrFailure && checkpoint.StreamPosition >= checkpoint.StreamVersion)
                    {
                        logger.CheckpointAlreadyCaughtUp(
                            checkpoint.Id,
                            checkpoint.StreamPosition,
                            instance,
                            group.Name
                        );

                        await database.Execute(
                            new ReleaseCheckpointReservation(checkpoint.Id),
                            CancellationToken.None
                        );

                        continue;
                    }

                    var subscription = group.GetSubscription(checkpoint.Name);

                    if (subscription == null)
                    {
                        logger.SubscriptionNotRegistered(
                            checkpoint.Name,
                            group.Name,
                            checkpoint.Id,
                            instance,
                            group.Name
                        );

                        await database.Execute(
                            new ReleaseCheckpointReservation(checkpoint.Id),
                            CancellationToken.None
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
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "Starting checkpoint polling for consumer {Consumer} in group {Group}")]
    public static partial void StartingCheckpointPolling(this ILogger logger, int consumer, string group);

    [LoggerMessage(0, LogLevel.Trace, "Attempting to reserve checkpoint for consumer {Consumer} in group {Group}")]
    public static partial void AttemptingToReserveCheckpoint(this ILogger logger, int consumer, string group);

    [LoggerMessage(0, LogLevel.Trace, "No available checkpoints - will continue to wait for consumer {Consumer} in group {Group}")]
    public static partial void NoAvailableCheckpoints(this ILogger logger, int consumer, string group);

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Skipping checkpoint {CheckpointId} - already caught up at stream position {StreamPosition} - releasing reservation and continuing polling in consumer {Consumer} in group {Group}"
    )]
    public static partial void CheckpointAlreadyCaughtUp(
        this ILogger logger,
        long checkpointId,
        long streamPosition,
        int consumer,
        string group
    );

    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Subscription {SubscriptionName} not registered for group {GroupName} - skipping checkpoint {CheckpointId} in consumer {Consumer} in group {Group}"
    )]
    public static partial void SubscriptionNotRegistered(
        this ILogger logger,
        string subscriptionName,
        string groupName,
        long checkpointId,
        int consumer,
        string group
    );
}
