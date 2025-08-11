using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class SubscriptionConfigurationSynchronizer(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    ILogger<SubscriptionConfigurationSynchronizer> logger
)
{
    public async Task SynchronizeSubscriptionConfigurations(
        IEnumerable<SubscriptionGroup> groups,
        CancellationToken cancellationToken = default)
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
                        new UpsertSubscriptionConfigurationNormalized(
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

                    logger.SynchronizedSubscriptionConfiguration(subscription.Name, group.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, 
                        "Failed to synchronize subscription configuration for {SubscriptionName} in group {GroupName}",
                        subscription.Name, group.Name);
                }
            }
        }
    }
}

public static partial class SubscriptionConfigurationLog
{
    [LoggerMessage(0, LogLevel.Debug, "Synchronized subscription configuration for {SubscriptionName} in group {GroupName}")]
    public static partial void SynchronizedSubscriptionConfiguration(this ILogger logger, string subscriptionName, string groupName);
}