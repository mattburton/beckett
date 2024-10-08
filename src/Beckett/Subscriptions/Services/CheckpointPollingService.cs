using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class CheckpointPollingService(
    SubscriptionOptions options,
    ICheckpointConsumerGroup consumerGroup
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumerGroup.Initialize(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            consumerGroup.StartPolling(options.GroupName);

            await Task.Delay(options.CheckpointPollingInterval, stoppingToken);
        }
    }
}
