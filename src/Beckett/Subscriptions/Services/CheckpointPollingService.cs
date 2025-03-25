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
        var timer = new PeriodicTimer(options.CheckpointPollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                logger.StartingCheckpointIntervalPolling();

                consumerGroup.Notify(options.GroupName);
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
    [LoggerMessage(0, LogLevel.Trace, "Starting checkpoint interval polling")]
    public static partial void StartingCheckpointIntervalPolling(this ILogger logger);
}
