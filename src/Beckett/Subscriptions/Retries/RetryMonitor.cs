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
                    new LockNextCheckpointForRetry(options.ApplicationName),
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
                    case CheckpointStatus.Retry:
                        await retryManager.StartRetry(
                            checkpoint.RetryId,
                            checkpoint.Application,
                            checkpoint.Name,
                            checkpoint.StreamName,
                            checkpoint.StreamPosition,
                            stoppingToken
                        );
                        break;
                    case CheckpointStatus.PendingFailure:
                        await retryManager.RecordFailure(
                            checkpoint.RetryId,
                            checkpoint.Application,
                            checkpoint.Name,
                            checkpoint.StreamName,
                            checkpoint.StreamPosition,
                            checkpoint.LastError,
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
        }
    }
}
