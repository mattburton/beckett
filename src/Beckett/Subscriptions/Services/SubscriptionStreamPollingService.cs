using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class SubscriptionStreamPollingService(
    SubscriptionOptions options,
    ISubscriptionStreamConsumerGroup consumerGroup
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.SubscriptionStreamPollingInterval == TimeSpan.Zero)
        {
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
