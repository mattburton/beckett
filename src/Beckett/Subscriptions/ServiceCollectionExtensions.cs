using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

public static class ServiceCollectionExtensions
{
    public static void AddSubscriptionSupport(this IServiceCollection services, BeckettOptions options)
    {
        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddSingleton<ISubscriptionRegistry>(options.Subscriptions.Registry);

        services.AddSingleton<ISubscriptionProcessor, SubscriptionProcessor>();
    }
}
