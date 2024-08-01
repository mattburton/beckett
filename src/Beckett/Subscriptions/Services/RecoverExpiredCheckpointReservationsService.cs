using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions.Services;

public class RecoverExpiredCheckpointReservationsService(
    IPostgresDatabase database,
    BeckettOptions options,
    ILogger<RecoverExpiredCheckpointReservationsService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var recovered = await database.Execute(
                    new RecoverExpiredCheckpointReservations(
                        options.Subscriptions.CheckpointReservationTimeout,
                        options.Subscriptions.CheckpointReservationRecoveryBatchSize
                    ),
                    stoppingToken
                );

                if (recovered <= 0)
                {
                    await Task.Delay(options.Subscriptions.CheckpointReservationRecoveryInterval, stoppingToken);
                }
                else
                {
                    logger.LogInformation("Recovered {Count} expired checkpoint reservation(s)", recovered);
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
