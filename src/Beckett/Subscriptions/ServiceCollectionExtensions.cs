using Beckett.Subscriptions.Initialization;
using Beckett.Subscriptions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

public static class ServiceCollectionExtensions
{
    public static void AddSubscriptionSupport(this IServiceCollection services, SubscriptionOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        services.AddSingleton(options);

        services.AddSingleton<ISubscriptionInitializer, SubscriptionInitializer>();

        services.AddSingleton<IGlobalStreamConsumer, GlobalStreamConsumer>();

        services.AddSingleton<ISubscriptionStreamProcessor, SubscriptionStreamProcessor>();

        services.AddSingleton<ISubscriptionConsumerGroup, SubscriptionConsumerGroup>();

        services.AddHostedService<BootstrapSubscriptions>();

        services.AddHostedService<GlobalPollingService>();

        services.AddHostedService<SubscriptionPollingService>();
    }
}
