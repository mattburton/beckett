using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class SubscriptionPollingService(
    SubscriptionOptions options,
    ISubscriptionConsumerGroup consumerGroup
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.PollingInterval == TimeSpan.Zero)
        {
            return;
        }

        consumerGroup.Initialize(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            consumerGroup.StartPolling(options.GroupName);

            await Task.Delay(options.PollingInterval, stoppingToken);
        }
    }
}
