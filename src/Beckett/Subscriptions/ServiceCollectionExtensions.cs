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

        services.AddSingleton<ICheckpointProcessor, CheckpointProcessor>();

        services.AddSingleton<IPostgresNotificationHandler, CheckpointNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, GlobalStreamNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, SubscriptionResetNotificationHandler>();

        services.AddHostedService<BootstrapSubscriptions>();

        services.AddHostedService<GroupSubscriptionInitializerHost>();

        services.AddHostedService<GlobalStreamConsumerHost>();

        services.AddHostedService<GlobalStreamPollingService>();

        services.AddHostedService<CheckpointConsumerGroupHost>();

        services.AddHostedService<CheckpointPollingService>();

        services.AddHostedService<StreamDataRecordingService>();

        services.AddHostedService<RecoverExpiredCheckpointReservationsService>();
    }
}
