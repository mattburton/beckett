using Beckett.Database;
using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class SubscriptionHost(
    BeckettOptions options,
    INotificationListener listener,
    SubscriptionStreamProcessor processor
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor.Initialize(stoppingToken);

        var tasks = new List<Task>();

        if (options.Subscriptions.UseNotifications)
        {
            tasks.Add(listener.Listen(
                "beckett:poll",
                (_, _) => processor.StartPolling(stoppingToken),
                stoppingToken
            ));
        }

        tasks.Add(PollingAgent.Run(options, processor, stoppingToken));

        return Task.WhenAll(tasks);
    }
}
