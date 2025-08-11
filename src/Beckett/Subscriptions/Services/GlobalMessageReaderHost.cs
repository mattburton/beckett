using Beckett.Database;
using Beckett.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class GlobalMessageReaderHost(
    BeckettOptions options,
    IGlobalStreamNotificationChannel channel,
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    IMessageStorage messageStorage,
    ISubscriptionConfigurationCache configurationCache,
    ILoggerFactory loggerFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a single global message reader
        var reader = new GlobalMessageReader(
            options,
            channel.Global, // Use a single global channel instead of per-group
            dataSource,
            database,
            messageStorage,
            configurationCache,
            loggerFactory.CreateLogger<GlobalMessageReader>()
        );

        await reader.Poll(stoppingToken);
    }
}