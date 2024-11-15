using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class CheckpointPollingService(
    SubscriptionOptions options,
    ICheckpointConsumerGroup consumerGroup,
    ILogger<CheckpointPollingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumerGroup.Initialize(stoppingToken);

        while (true)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                logger.StartingCheckpointIntervalPolling();

                consumerGroup.StartPolling(options.GroupName);

                await Task.Delay(options.CheckpointPollingInterval, stoppingToken);
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
    [LoggerMessage(0, LogLevel.Trace, "Starting checkpoint interval polling")]
    public static partial void StartingCheckpointIntervalPolling(this ILogger logger);
}
