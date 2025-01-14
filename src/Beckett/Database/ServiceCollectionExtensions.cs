using Beckett.Database.Notifications;
using Beckett.Database.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Database;

public static class ServiceCollectionExtensions
{
    internal static void AddPostgresSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Postgres);

        services.AddSingleton<IPostgresDataSource, PostgresDataSource>();

        services.AddSingleton<IPostgresDatabase, PostgresDatabase>();

        if (!options.Postgres.Notifications)
        {
            return;
        }

        services.AddSingleton<IPostgresNotificationListener>(
            provider =>
            {
                var dataSource = options.Postgres.NotificationsDataSource ??
                                 options.Postgres.DataSource ?? provider.GetService<NpgsqlDataSource>();

                if (dataSource is null)
                {
                    throw new Exception(
                        "Registered NpgsqlDataSource not found - please register one using AddNpgsqlDataSource from the Npgsql.DependencyInjection package, provide a configured instance via UseNotificationsDataSource, or call UseNotificationsConnectionString"
                    );
                }

                return new PostgresNotificationListener(
                    dataSource,
                    provider.GetServices<IPostgresNotificationHandler>(),
                    provider.GetRequiredService<ILogger<PostgresNotificationListener>>()
                );
            }
        );

        services.AddHostedService<PostgresNotificationListenerService>();
    }
}
