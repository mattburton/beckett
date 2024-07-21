using Beckett.Database.Notifications;
using Beckett.Database.Notifications.Handlers;
using Beckett.Database.Notifications.Services;
using Beckett.Messages.Storage;
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

        services.AddSingleton<IPostgresMessageDeserializer, PostgresMessageDeserializer>();

        services.AddSingleton(typeof(IMessageStorage), options.Postgres.MessageStorageType);

        if (options.Postgres.Notifications && options.Subscriptions.Enabled)
        {
            AddPostgresNotifications(services);
        }
    }

    private static void AddPostgresNotifications(IServiceCollection services)
    {
        services.AddSingleton<IPostgresNotificationHandler, CheckpointNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, MessageNotificationHandler>();

        services.AddSingleton<IPostgresNotificationHandler, RetryNotificationHandler>();

        services.AddSingleton<IPostgresNotificationListener, PostgresNotificationListener>();

        services.AddHostedService<PostgresNotificationListenerService>();
    }
}
