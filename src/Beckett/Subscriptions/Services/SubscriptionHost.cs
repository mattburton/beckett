using Beckett.Storage;
using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class SubscriptionHost(
    BeckettOptions options,
    IStorageProvider storageProvider,
    ISubscriptionStreamProcessor processor
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor.Initialize(stoppingToken);

        var tasks = new List<Task>();

        tasks.AddRange(storageProvider.GetSubscriptionHostTasks(processor, stoppingToken));

        tasks.Add(PollingAgent.Run(options, processor, stoppingToken));

        return Task.WhenAll(tasks);
    }
}
