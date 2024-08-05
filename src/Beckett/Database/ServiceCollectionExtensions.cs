using Beckett.Database.Notifications;
using Beckett.Database.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Beckett.Database;

public static class ServiceCollectionExtensions
{
    public static void AddPostgresSupport(this IServiceCollection services, BeckettOptions options)
    {
        if (!options.Postgres.Enabled)
        {
            return;
        }

        services.AddSingleton(options);

        services.AddSingleton<IPostgresDatabase>(
            provider =>
            {
                var dataSource = options.Postgres.DataSource ?? provider.GetService<NpgsqlDataSource>();

                if (dataSource is null)
                {
                    throw new Exception(
                        "Registered NpgsqlDataSource not found - please register one using AddNpgsqlDataSource from the Npgsql.DependencyInjection package, provide a configured instance via UseDataSource, or call UseConnectionString"
                    );
                }

                return new PostgresDatabase(dataSource, options.Postgres);
            }
        );

        if (!options.Postgres.Notifications)
        {
            return;
        }

        services.AddSingleton<IPostgresNotificationListener, PostgresNotificationListener>();

        services.AddHostedService<PostgresNotificationListenerService>();
    }
}
