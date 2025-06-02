using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class RecoverExpiredCheckpointReservationsService(
    IPostgresDatabase database,
    BeckettOptions options,
    ILogger<RecoverExpiredCheckpointReservationsService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = options.Subscriptions.Groups.Select(x => ExecuteForSubscriptionGroup(x, stoppingToken)).ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteForSubscriptionGroup(SubscriptionGroup group, CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                var recovered = await database.Execute(
                    new RecoverExpiredCheckpointReservations(
                        group.Name,
                        group.ReservationRecoveryBatchSize,
                        options.Postgres
                    ),
                    stoppingToken
                );

                if (recovered <= 0)
                {
                    await Task.Delay(group.ReservationRecoveryInterval, stoppingToken);
                }
                else
                {
                    logger.RecoveredExpiredCheckpointReservations(recovered, group.Name);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled error - will retry in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Information, "Recovered {Count} expired checkpoint reservation(s) for group {GroupName}")]
    public static partial void RecoveredExpiredCheckpointReservations(this ILogger logger, int count, string groupName);
}
