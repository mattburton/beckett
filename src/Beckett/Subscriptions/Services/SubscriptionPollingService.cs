using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class SubscriptionPollingService(BeckettOptions options, ISubscriptionProcessor processor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor.Initialize(stoppingToken);

        if (options.Subscriptions.PollingInterval == TimeSpan.Zero)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            processor.Poll(stoppingToken);

            await Task.Delay(options.Subscriptions.PollingInterval, stoppingToken);
        }
    }
}
