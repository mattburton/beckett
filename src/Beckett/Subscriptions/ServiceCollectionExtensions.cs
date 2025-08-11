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

        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddSingleton<ISubscriptionInitializerChannel, SubscriptionInitializerChannel>();

        services.AddSingleton<IGlobalStreamNotificationChannel, GlobalStreamNotificationChannel>();

        services.AddSingleton<ICheckpointNotificationChannel, CheckpointNotificationChannel>();

        services.AddSingleton<ISubscriptionRegistry, SubscriptionRegistry>();

        services.AddSingleton<ICheckpointProcessor, CheckpointProcessor>();

        services.AddSingleton<ISubscriptionConfigurationCache, SubscriptionConfigurationCache>();

        services.AddSingleton<SubscriptionConfigurationSynchronizer>();

        services.AddSingleton<IPostgresNotificationHandler, CheckpointNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, GlobalStreamNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, SubscriptionResetNotificationHandler>();

        services.AddHostedService<BootstrapSubscriptions>();

        services.AddHostedService<GroupSubscriptionInitializerHost>();

        // Use new GlobalMessageReader for single global processing
        services.AddHostedService<GlobalMessageReaderHost>();

        services.AddHostedService<GlobalStreamPollingService>();

        services.AddHostedService<CheckpointConsumerGroupHost>();

        services.AddHostedService<CheckpointPollingService>();

        services.AddHostedService<RecoverExpiredCheckpointReservationsService>();
    }
}
