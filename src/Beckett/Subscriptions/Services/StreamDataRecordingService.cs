using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class StreamDataRecordingService(
    IPostgresDatabase database,
    BeckettOptions options,
    ILogger<StreamDataRecordingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var data in StreamDataQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await database.ExecuteWithRetry(
                    new RecordStreamData(data.Categories, data.CategoryTimestamps, data.Tenants, options.Postgres),
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while recording stream data");
            }
        }
    }
}
