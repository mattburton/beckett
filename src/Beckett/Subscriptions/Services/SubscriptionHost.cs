using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class SubscriptionHost(
    BeckettOptions options,
    ISubscriptionStorage storage,
    ISubscriptionProcessor processor
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor.Initialize(stoppingToken);

        var tasks = new List<Task>();

        tasks.AddRange(storage.ConfigureSubscriptionHost(processor, stoppingToken));

        tasks.Add(ContinuousPolling(processor, options, stoppingToken));

        return Task.WhenAll(tasks);
    }

    private static async Task ContinuousPolling(
        ISubscriptionProcessor processor,
        BeckettOptions options,
        CancellationToken cancellationToken
    )
    {
        if (options.Subscriptions.PollingInterval == TimeSpan.Zero)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            processor.Poll(cancellationToken);

            await Task.Delay(options.Subscriptions.PollingInterval, cancellationToken);
        }
    }
}
