using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class GlobalStreamPollingService(
    BeckettOptions options,
    IGlobalStreamNotificationChannel channel,
    ILogger<GlobalStreamPollingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = options.Subscriptions.Groups.Select(x => ExecuteForSubscriptionGroup(x, stoppingToken)).ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteForSubscriptionGroup(SubscriptionGroup group, CancellationToken stoppingToken)
    {
        if (group.GlobalStreamPollingInterval == TimeSpan.Zero)
        {
            logger.LogInformation("Disabling global stream polling - the polling interval is set to zero.");

            return;
        }

        var timer = new PeriodicTimer(group.GlobalStreamPollingInterval);

        var groupChannel = channel.For(group.Name);

        //start polling immediately then poll on an interval after that
        await TriggerPollingForGroup(group, groupChannel, stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TriggerPollingForGroup(group, groupChannel, stoppingToken);
        }
    }

    private async Task TriggerPollingForGroup(
        SubscriptionGroup group,
        Channel<MessagesAvailable> groupChannel,
        CancellationToken stoppingToken
    )
    {
        try
        {
            logger.StartingGlobalStreamIntervalPolling(group.Name);

            groupChannel.Writer.TryWrite(MessagesAvailable.Instance);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled error - will retry in 10 seconds");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public static partial class Log
{
    [LoggerMessage(0, LogLevel.Trace, "Starting global stream interval polling for group {GroupName}")]
    public static partial void StartingGlobalStreamIntervalPolling(this ILogger logger, string groupName);
}
