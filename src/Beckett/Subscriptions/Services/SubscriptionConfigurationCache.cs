using System.Collections.Concurrent;
using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions.Services;

public class SubscriptionConfigurationCache(
    IPostgresDatabase database,
    ILogger<SubscriptionConfigurationCache> logger)
    : ISubscriptionConfigurationCache
{
    private readonly ConcurrentDictionary<string, GetAllSubscriptionConfigurationsNormalized.Result> _configurations = new();
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private volatile bool _isInitialized = false;

    public async Task<IReadOnlyList<GetAllSubscriptionConfigurationsNormalized.Result>> GetConfigurations(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await RefreshCache(cancellationToken);
        }

        return _configurations.Values.ToArray();
    }

    public void InvalidateCache()
    {
        logger.LogDebug("Invalidating subscription configuration cache");
        _configurations.Clear();
        _isInitialized = false;
    }

    public async Task RefreshCache(CancellationToken cancellationToken = default)
    {
        await _refreshSemaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogDebug("Refreshing subscription configuration cache");

            var configurations = await database.Execute(
                new GetAllSubscriptionConfigurationsNormalized(),
                cancellationToken
            );

            _configurations.Clear();

            foreach (var config in configurations)
            {
                var key = $"{config.GroupName}:{config.SubscriptionName}";
                _configurations[key] = config;
            }

            _isInitialized = true;

            logger.LogDebug("Loaded {Count} subscription configurations into cache", configurations.Count);
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }
}
