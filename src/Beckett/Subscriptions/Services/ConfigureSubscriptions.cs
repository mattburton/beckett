using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Hosting;

namespace Beckett.Subscriptions.Services;

public class ConfigureSubscriptions(IDataSource dataSource) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(stoppingToken);

        foreach (var subscription in SubscriptionRegistry.All())
        {
            await AddOrUpdateSubscriptionQuery.Execute(
                connection,
                subscription.Name,
                subscription.EventTypes,
                subscription.StartingPosition == StartingPosition.Earliest,
                stoppingToken
            );
        }
    }
}
