using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class GlobalStreamPollingService(
    IGlobalStreamConsumer globalStreamConsumer,
    SubscriptionOptions options,
    ILogger<GlobalStreamPollingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.GlobalStreamPollingInterval == TimeSpan.Zero)
        {
            logger.LogInformation("Disabling global stream polling - the polling interval is set to zero.");

            return;
        }

        var timer = new PeriodicTimer(options.GlobalStreamPollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                logger.StartingGlobalStreamIntervalPolling();

                globalStreamConsumer.Notify();
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
    [LoggerMessage(0, LogLevel.Trace, "Starting global stream interval polling")]
    public static partial void StartingGlobalStreamIntervalPolling(this ILogger logger);
}
