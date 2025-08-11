using System.Collections.Concurrent;
using Beckett.Database;
using Beckett.Subscriptions.Queries;

namespace Beckett.Subscriptions.Services;

public class SubscriptionConfigurationCache(IPostgresDatabase database) : ISubscriptionConfigurationCache
{
    private readonly ConcurrentDictionary<string, SubscriptionConfiguration> _configurations = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private volatile bool _initialized;

    public async Task<IReadOnlyList<SubscriptionConfiguration>> GetConfigurations(
        CancellationToken cancellationToken = default
    )
    {
        if (_initialized)
        {
            return _configurations.Values.ToArray();
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!_initialized)
            {
                await RefreshCacheInternal(cancellationToken);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return _configurations.Values.ToArray();
    }

    public void InvalidateCache()
    {
        _configurations.Clear();
        _initialized = false;
    }

    public async Task RefreshCache(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            await RefreshCacheInternal(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshCacheInternal(CancellationToken cancellationToken)
    {
        var configurations = await database.Execute(
            new GetSubscriptionConfigurations(),
            cancellationToken
        );

        _configurations.Clear();

        foreach (var config in configurations)
        {
            var key = $"{config.GroupName}:{config.SubscriptionName}";
            _configurations[key] = config;
        }

        _initialized = true;
    }
}
