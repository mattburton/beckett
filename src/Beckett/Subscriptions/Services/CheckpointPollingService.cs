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
        if (options.CheckpointPollingInterval == TimeSpan.Zero)
        {
            logger.LogInformation("Disabling checkpoint polling - the polling interval is set to zero.");

            return;
        }

        consumerGroup.Initialize(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            consumerGroup.StartPolling(options.GroupName);

            await Task.Delay(options.CheckpointPollingInterval, stoppingToken);
        }
    }
}
