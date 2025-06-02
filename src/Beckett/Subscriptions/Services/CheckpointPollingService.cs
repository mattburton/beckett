using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class CheckpointPollingService(
    BeckettOptions options,
    ICheckpointNotificationChannel channel,
    ILogger<CheckpointPollingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = options.Subscriptions.Groups.Select(x => ExecuteForSubscriptionGroup(x, stoppingToken)).ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteForSubscriptionGroup(SubscriptionGroup group, CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(group.CheckpointPollingInterval);

        var groupChannel = channel.For(group.Name);

        //start polling immediately then poll on an interval after that
        await TriggerPollingForGroup(groupChannel, stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TriggerPollingForGroup(groupChannel, stoppingToken);
        }
    }

    private async Task TriggerPollingForGroup(
        Channel<CheckpointAvailable> groupChannel,
        CancellationToken stoppingToken
    )
    {
        try
        {
            logger.StartingCheckpointIntervalPolling();

            groupChannel.Writer.TryWrite(CheckpointAvailable.Instance);
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
    [LoggerMessage(0, LogLevel.Trace, "Starting checkpoint interval polling")]
    public static partial void StartingCheckpointIntervalPolling(this ILogger logger);
}
