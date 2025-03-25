using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class CheckpointConsumerGroupHost(ICheckpointConsumerGroup consumerGroup) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consumerGroup.Poll(stoppingToken);
    }
}
