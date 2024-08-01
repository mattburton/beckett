using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions.Retries;

public class RetryMonitor(
    IPostgresDatabase database,
    BeckettOptions options,
    IRetryManager retryManager,
    ILogger<RetryMonitor> logger
) : IRetryMonitor
{
    private Task _task = Task.CompletedTask;

    public void StartPolling(CancellationToken stoppingToken)
    {
        if (_task is { IsCompleted: false })
        {
            return;
        }

        _task = Poll(stoppingToken);
    }

    private async Task Poll(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var checkpoint = await database.Execute(
                    new LockNextCheckpointForRetry(options.Subscriptions.GroupName),
                    connection,
                    transaction,
                    stoppingToken
                );

                if (checkpoint == null)
                {
                    break;
                }

                switch (checkpoint.Status)
                {
                    case CheckpointStatus.RetryPending:
                        await retryManager.StartRetry(
                            checkpoint.Id,
                            checkpoint.Name,
                            checkpoint.StreamName,
                            checkpoint.StreamPosition,
                            checkpoint.LastError,
                            connection,
                            transaction,
                            stoppingToken
                        );
                        break;
                    case CheckpointStatus.FailurePending:
                        await retryManager.RecordFailure(
                            checkpoint.Id,
                            checkpoint.Name,
                            checkpoint.StreamName,
                            checkpoint.StreamPosition,
                            checkpoint.LastError,
                            connection,
                            transaction,
                            stoppingToken
                        );
                        break;
                }

                await transaction.CommitAsync(stoppingToken);
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
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled exception in retry monitor");
            }
        }
    }
}
