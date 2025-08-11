using Beckett.Subscriptions.Queries;

namespace Beckett.Subscriptions.Services;

public interface ISubscriptionConfigurationCache
{
    Task<IReadOnlyList<GetAllSubscriptionConfigurationsNormalized.Result>> GetConfigurations(CancellationToken cancellationToken = default);
    void InvalidateCache();
    Task RefreshCache(CancellationToken cancellationToken = default);
}