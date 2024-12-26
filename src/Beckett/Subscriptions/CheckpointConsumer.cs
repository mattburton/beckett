using System.Threading.Channels;
using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class CheckpointConsumer(
    Channel<CheckpointAvailable> channel,
    int instance,
    IPostgresDatabase database,
    ICheckpointProcessor checkpointProcessor,
    BeckettOptions options,
    ILogger<CheckpointConsumer> logger
) : ICheckpointConsumer
{
    public async Task StartPolling(CancellationToken cancellationToken)
    {
        logger.StartingCheckpointPolling(instance);

        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            try
            {
                if (!channel.Reader.TryRead(out _))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                logger.AttemptingToReserveCheckpoint(instance);

                var checkpoint = await database.Execute(
                    new ReserveNextAvailableCheckpoint(
                        options.Subscriptions.GroupName,
                        options.Subscriptions.ReservationTimeout,
                        options.Postgres
                    ),
                    cancellationToken
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
                        cancellationToken
                    );

                    continue;
                }

                var subscription = SubscriptionRegistry.GetSubscription(checkpoint.Name);

                if (subscription == null)
                {
                    logger.SubscriptionNotRegistered(checkpoint.Name, options.Subscriptions.GroupName, checkpoint.Id, instance);

                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                await checkpointProcessor.Process(
                    instance,
                    checkpoint,
                    subscription,
                    cancellationToken
                );

                channel.Writer.TryWrite(CheckpointAvailable.Instance);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
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

    [LoggerMessage(0, LogLevel.Trace, "Skipping checkpoint {CheckpointId} - already caught up at stream position {StreamPosition} - releasing reservation and continuing polling in consumer {Consumer}")]
    public static partial void CheckpointAlreadyCaughtUp(this ILogger logger, long checkpointId, long streamPosition, int consumer);

    [LoggerMessage(0, LogLevel.Trace, "Subscription {SubscriptionName} not registered for group {GroupName} - skipping checkpoint {CheckpointId} in consumer {Consumer}")]
    public static partial void SubscriptionNotRegistered(this ILogger logger, string subscriptionName, string groupName, long checkpointId, int consumer);
}
