using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class SubscriptionConfigurationSynchronizer(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    ISubscriptionConfigurationCache configurationCache,
    ILogger<SubscriptionConfigurationSynchronizer> logger
)
{
    public async Task Synchronize(IEnumerable<SubscriptionGroup> groups, CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        foreach (var group in groups)
        {
            foreach (var subscription in group.GetSubscriptions())
            {
                try
                {
                    await database.Execute(
                        new UpsertSubscriptionConfiguration(
                            group.Name,
                            subscription.Name,
                            subscription.Category,
                            subscription.StreamName,
                            subscription.MessageTypeNames.ToArray(),
                            subscription.Priority,
                            subscription.SkipDuringReplay
                        ),
                        connection,
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to synchronize subscription configuration for {SubscriptionName} in group {GroupName}",
                        subscription.Name,
                        group.Name
                    );
                }
            }
        }

        await configurationCache.RefreshCache(cancellationToken);
    }
}
