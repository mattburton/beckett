using Beckett.Database.Notifications;
using Beckett.Database.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Beckett.Database;

public static class ServiceCollectionExtensions
{
    internal static void AddPostgresSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Postgres);

        services.AddSingleton<IPostgresDataSource, PostgresDataSource>();

        services.AddSingleton<IPostgresDatabase, PostgresDatabase>();

        if (options.Postgres.NotificationsDataSource == null)
        {
            return;
        }

        services.AddSingleton<IPostgresNotificationListener>(
            provider => new PostgresNotificationListener(
                options.Postgres.NotificationsDataSource,
                provider.GetServices<IPostgresNotificationHandler>(),
                provider.GetRequiredService<ILogger<PostgresNotificationListener>>()
            )
        );

        services.AddHostedService<PostgresNotificationListenerService>();
    }
}
