using Beckett.Database.Notifications;
using Beckett.Subscriptions.Initialization;
using Beckett.Subscriptions.NotificationHandlers;
using Beckett.Subscriptions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

public static class ServiceCollectionExtensions
{
    internal static void AddSubscriptionSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Subscriptions);

        services.AddSingleton<ISubscriptionInitializer, SubscriptionInitializer>();

        services.AddSingleton<IGlobalStreamConsumer, GlobalStreamConsumer>();

        services.AddSingleton<ICheckpointProcessor, CheckpointProcessor>();

        services.AddSingleton<ICheckpointConsumerGroup, CheckpointConsumerGroup>();

        services.AddSingleton<IPostgresNotificationHandler, CheckpointNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, MessageNotificationHandler>();

        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddHostedService<BootstrapSubscriptions>();

        services.AddHostedService<GlobalStreamConsumerHost>();

        services.AddHostedService<GlobalStreamPollingService>();

        services.AddHostedService<CheckpointConsumerGroupHost>();

        services.AddHostedService<CheckpointPollingService>();

        services.AddHostedService<RecoverExpiredCheckpointReservationsService>();
    }
}
