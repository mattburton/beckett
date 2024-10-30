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

        while (true)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                globalStreamConsumer.StartPolling(stoppingToken);

                await Task.Delay(options.GlobalStreamPollingInterval, stoppingToken);
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
