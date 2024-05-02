using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

internal static class ServiceCollectionExtensions
{
    public static void AddSubscriptionSupport(this IServiceCollection services, BeckettOptions options)
    {
        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddSingleton<ISubscriptionProcessor, SubscriptionProcessor>();
    }
}
