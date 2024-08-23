using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class SubscriptionStreamPollingService(
    SubscriptionOptions options,
    ISubscriptionStreamConsumerGroup consumerGroup,
    ILogger<SubscriptionStreamPollingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.SubscriptionStreamPollingInterval == TimeSpan.Zero)
        {
            logger.LogInformation("Disabling subscription stream polling - the polling interval is set to zero.");

            return;
        }

        consumerGroup.Initialize(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            consumerGroup.StartPolling(options.GroupName);

            await Task.Delay(options.SubscriptionStreamPollingInterval, stoppingToken);
        }
    }
}
