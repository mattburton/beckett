namespace Beckett.Subscriptions.Services;

public interface ISubscriptionConfigurationCache
{
    Task<IReadOnlyList<SubscriptionConfiguration>> GetConfigurations(CancellationToken cancellationToken = default);
    void InvalidateCache();
    Task RefreshCache(CancellationToken cancellationToken = default);
}
