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

        services.AddSingleton<IGlobalStreamConsumer, GlobalStreamConsumer>();

        services.AddSingleton<ISubscriptionStreamProcessor, SubscriptionStreamProcessor>();

        services.AddSingleton<ISubscriptionConsumerGroup, SubscriptionConsumerGroup>();

        services.AddHostedService<InitializeSubscriptions>();

        services.AddHostedService<GlobalPollingService>();

        services.AddHostedService<SubscriptionPollingService>();
    }
}
