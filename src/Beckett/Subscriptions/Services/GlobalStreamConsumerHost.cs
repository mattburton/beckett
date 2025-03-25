using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class GlobalStreamConsumerHost(IGlobalStreamConsumer globalStreamConsumer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await globalStreamConsumer.Poll(stoppingToken);
    }
}
