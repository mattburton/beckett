using Beckett.Subscriptions.Services;
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

        services.AddSingleton<ISubscriptionProcessor, SubscriptionProcessor>();

        services.AddHostedService<ConfigureSubscriptions>();

        services.AddHostedService<SubscriptionHost>();
    }
}
