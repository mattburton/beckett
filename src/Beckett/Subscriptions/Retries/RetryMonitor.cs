using Beckett.Database;
using Beckett.Subscriptions.Retries.Queries;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions.Retries;

public class RetryMonitor(
    IPostgresDatabase database,
    BeckettOptions options,
    IRetryProcessor retryProcessor,
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
                var retry = await database.Execute(
                    new ReserveNextAvailableRetry(
                        options.Subscriptions.GroupName,
                        options.Subscriptions.ReservationTimeout
                    ),
                    stoppingToken
                );

                if (retry == null)
                {
                    break;
                }

                switch (retry.Status)
                {
                    case RetryStatus.Started:
                        if (retry.MaxRetryCount == 0)
                        {
                            await database.Execute(
                                new RecordRetryEvent(retry.Id, RetryStatus.Failed, retry.Attempts),
                                stoppingToken
                            );
                        }
                        else
                        {
                            var retryAt = 1.GetNextDelayWithExponentialBackoff(
                                options.Subscriptions.Retries.InitialDelay,
                                options.Subscriptions.Retries.MaxDelay
                            );

                            await database.Execute(
                                new RecordRetryEvent(retry.Id, RetryStatus.Scheduled, retry.Attempts, retryAt),
                                stoppingToken
                            );
                        }
                        break;
                    case RetryStatus.Scheduled:
                    case RetryStatus.ManualRetryRequested:
                        await retryProcessor.Retry(
                            retry.Id,
                            retry.Name,
                            retry.StreamName,
                            retry.StreamPosition,
                            retry.Attempts.GetValueOrDefault(),
                            retry.MaxRetryCount,
                            retry.Status == RetryStatus.ManualRetryRequested,
                            stoppingToken
                        );
                        break;
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
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled exception in retry monitor");
            }
        }
    }
}
