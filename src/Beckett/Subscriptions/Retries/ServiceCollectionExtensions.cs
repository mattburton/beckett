using Beckett.Database.Notifications;
using Beckett.Subscriptions.Retries.NotificationHandlers;
using Beckett.Subscriptions.Retries.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class ServiceCollectionExtensions
{
    public static void AddRetrySupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Subscriptions.Retries);

        services.AddSingleton<IRetryClient, RetryClient>();

        services.AddSingleton<IRetryMonitor, RetryMonitor>();

        services.AddSingleton<IRetryProcessor, RetryProcessor>();

        if (!options.Subscriptions.Retries.Enabled)
        {
            return;
        }

        services.AddSingleton<IPostgresNotificationHandler, RetryNotificationHandler>();

        services.AddHostedService<RetryPollingService>();

        services.AddHostedService<RecoverExpiredRetryReservationsService>();
    }
}
