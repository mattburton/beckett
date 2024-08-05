using Beckett.Database.Notifications;
using Beckett.Subscriptions.Initialization;
using Beckett.Subscriptions.NotificationHandlers;
using Beckett.Subscriptions.Retries;
using Beckett.Subscriptions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

public static class ServiceCollectionExtensions
{
    public static void AddSubscriptionSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Subscriptions);

        services.AddSingleton<IRetryClient, RetryClient>();

        services.AddSingleton<ISubscriptionInitializer, SubscriptionInitializer>();

        services.AddSingleton<IGlobalStreamConsumer, GlobalStreamConsumer>();

        services.AddSingleton<ISubscriptionStreamProcessor, SubscriptionStreamProcessor>();

        services.AddSingleton<ISubscriptionStreamConsumerGroup, SubscriptionStreamConsumerGroup>();

        services.AddSingleton<IPostgresNotificationHandler, CheckpointNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, MessageNotificationHandler>();

        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddHostedService<BootstrapSubscriptions>();

        services.AddHostedService<GlobalStreamPollingService>();

        services.AddHostedService<SubscriptionStreamPollingService>();

        services.AddHostedService<RecoverExpiredCheckpointReservationsService>();
    }
}
