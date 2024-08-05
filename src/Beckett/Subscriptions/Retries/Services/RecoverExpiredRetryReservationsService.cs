using Beckett.Database;
using Beckett.Subscriptions.Retries.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions.Retries.Services;

public class RecoverExpiredRetryReservationsService(
    IPostgresDatabase database,
    BeckettOptions options,
    ILogger<RecoverExpiredRetryReservationsService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var recovered = await database.Execute(
                    new RecoverExpiredRetryReservations(
                        options.Subscriptions.ReservationTimeout,
                        options.Subscriptions.ReservationRecoveryBatchSize
                    ),
                    stoppingToken
                );

                if (recovered <= 0)
                {
                    await Task.Delay(options.Subscriptions.ReservationRecoveryInterval, stoppingToken);
                }
                else
                {
                    logger.LogInformation("Recovered {Count} expired retry reservation(s)", recovered);
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (NpgsqlException e)
            {
                logger.LogError(e, "Database error - will retry in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
