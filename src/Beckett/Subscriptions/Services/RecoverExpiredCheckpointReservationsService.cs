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
        while (true)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                var recovered = await database.Execute(
                    new RecoverExpiredCheckpointReservations(
                        options.Subscriptions.GroupId,
                        options.Subscriptions.ReservationRecoveryBatchSize,
                        options.Postgres
                    ),
                    stoppingToken
                );

                if (recovered <= 0)
                {
                    await Task.Delay(options.Subscriptions.ReservationRecoveryInterval, stoppingToken);
                }
                else
                {
                    logger.RecoveredExpiredCheckpointReservations(recovered);
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
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
    [LoggerMessage(0, LogLevel.Information, "Recovered {Count} expired checkpoint reservation(s)")]
    public static partial void RecoveredExpiredCheckpointReservations(this ILogger logger, int count);
}
