using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Subscriptions.Services;

public class GlobalPollingService(
    IGlobalStreamConsumer globalStreamConsumer,
    SubscriptionOptions options,
    ILogger<GlobalPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                globalStreamConsumer.StartPolling(stoppingToken);

                await Task.Delay(options.GlobalPollingInterval, stoppingToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (NpgsqlException e)
            {
                logger.LogError(e, "Database error - will retry in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
