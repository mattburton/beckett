using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class StreamDataRecordingService(
    IPostgresDatabase database,
    ILogger<StreamDataRecordingService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in StreamDataQueue.Reader.BatchReadAllAsync(100, TimeSpan.FromSeconds(10))
                           .WithCancellation(stoppingToken))
        {
            try
            {
                if (batch.Length == 0)
                {
                    continue;
                }

                var categories = new Dictionary<string, DateTimeOffset>();
                var tenants = new HashSet<string>();

                foreach (var item in batch)
                {
                    foreach (var category in item.Categories.Select((Value, Index) => (Value, Index)))
                    {
                        categories[category.Value] = item.CategoryTimestamps[category.Index];
                    }

                    foreach (var tenant in item.Tenants)
                    {
                        tenants.Add(tenant);
                    }
                }

                await database.Execute(
                    new RecordStreamData(
                        categories.Keys.ToArray(),
                        categories.Values.ToArray(),
                        tenants.ToArray()
                    ),
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
