using Beckett.Database;
using Beckett.Scheduling;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Models;
using Beckett.Subscriptions.Retries.Queries;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions.Retries;

public class RetryMonitor(
    IPostgresDatabase database,
    BeckettOptions options,
    IMessageStore messageStore,
    ITransactionalMessageScheduler messageScheduler,
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

                var retry = await database.Execute(
                    new LockNextPendingRetry(options.Subscriptions.GroupName, options.Postgres),
                    connection,
                    transaction,
                    stoppingToken
                );

                if (retry == null)
                {
                    break;
                }

                var subscription = SubscriptionRegistry.GetSubscription(retry.Name);

                if (subscription == null)
                {
                    logger.LogWarning("Skipping unknown subscription: {Name}", retry.Name);

                    continue;
                }

                var exceptionType = ExceptionTypeProvider.FindMatchFor(x => x.FullName == retry.Error.Type);

                if (exceptionType == null)
                {
                    logger.LogWarning("Unknown exception type: {Type} - using Exception instead", retry.Error.Type);

                    exceptionType = typeof(Exception);
                }

                var retryStreamName = RetryStreamName.For(retry.Id);

                var maxRetryCount = subscription.GetMaxRetryCount(options, exceptionType);

                if (maxRetryCount == 0)
                {
                    await RecordImmediateFailure(connection, transaction, retryStreamName, retry, stoppingToken);
                }
                else
                {
                    await ScheduleFirstAttempt(
                        connection,
                        transaction,
                        retryStreamName,
                        retry,
                        maxRetryCount,
                        stoppingToken
                    );
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

    private async Task ScheduleFirstAttempt(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string retryStreamName,
        Retry retry,
        int maxRetryCount,
        CancellationToken stoppingToken
    )
    {
        var retryAt = 1.GetNextDelayWithExponentialBackoff(
            options.Subscriptions.Retries.InitialDelay,
            options.Subscriptions.Retries.MaxDelay
        );

        logger.LogTrace(
            "Retry received for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - will begin retrying at {RetryAt}",
            options.Subscriptions.GroupName,
            retry.Name,
            retry.StreamName,
            retry.StreamPosition,
            retryAt
        );

        await messageStore.AppendToStream(
            retryStreamName,
            ExpectedVersion.Any,
            new RetryStarted(
                retry.Id,
                retry.GroupName,
                retry.Name,
                retry.StreamName,
                retry.StreamPosition,
                retry.Error,
                maxRetryCount,
                retryAt,
                DateTimeOffset.UtcNow
            ),
            stoppingToken
        );

        await database.Execute(
            new UpdateCheckpointStatus(
                retry.GroupName,
                retry.Name,
                retry.StreamName,
                retry.StreamPosition,
                CheckpointStatus.Retry,
                options.Postgres
            ),
            connection,
            transaction,
            stoppingToken
        );

        await messageScheduler.ScheduleMessage(
            retryStreamName,
            new Message(
                new RetryScheduled(
                    retry.Id,
                    1,
                    retryAt,
                    DateTimeOffset.UtcNow
                )
            ),
            retryAt,
            connection,
            transaction,
            stoppingToken
        );
    }

    private async Task RecordImmediateFailure(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string retryStreamName,
        Retry retry,
        CancellationToken stoppingToken
    )
    {
        logger.LogTrace(
            "Retry received for checkpoint {GroupName}:{Name}:{StreamName} at position {StreamPosition} - max retry count is set to zero for exception type {ExceptionType}, setting it to failed immediately",
            options.Subscriptions.GroupName,
            retry.Name,
            retry.StreamName,
            retry.StreamPosition,
            retry.Error.Type
        );

        await messageStore.AppendToStream(
            retryStreamName,
            ExpectedVersion.Any,
            [
                new RetryStarted(
                    retry.Id,
                    retry.GroupName,
                    retry.Name,
                    retry.StreamName,
                    retry.StreamPosition,
                    retry.Error,
                    0,
                    null,
                    DateTimeOffset.UtcNow
                ),
                new RetryFailed(
                    retry.Id,
                    0,
                    retry.Error,
                    DateTimeOffset.UtcNow
                )
            ],
            stoppingToken
        );

        await database.Execute(
            new UpdateCheckpointStatus(
                retry.GroupName,
                retry.Name,
                retry.StreamName,
                retry.StreamPosition,
                CheckpointStatus.Failed,
                options.Postgres
            ),
            connection,
            transaction,
            stoppingToken
        );
    }
}
