using Beckett.Events;
using Beckett.Events.Scheduling;
using Beckett.Storage.Postgres.Services;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Beckett.Storage.Postgres;

public static class ServiceCollectionExtensions
{
    public static void AddPostgresSupport(this IServiceCollection services, BeckettOptions options)
    {
        if (!options.Postgres.Enabled)
        {
            return;
        }

        services.AddSingleton<IPostgresDatabase>(provider =>
        {
            var dataSource = options.Postgres.DataSource ?? provider.GetService<NpgsqlDataSource>();
            if (dataSource is null)
            {
                throw new InvalidOperationException(
                    "Registered NpgsqlDataSource not found - please register one using AddNpgsqlDataSource from the Npgsql.DependencyInjection package, provide a configured instance via UseDataSource, or call UseConnectionString"
                );
            }
            return new PostgresDatabase(dataSource);
        });

        services.AddSingleton<IEventStorage, PostgresEventStorage>();

        services.AddSingleton<IPostgresEventStorage, PostgresEventStorage>();

        services.AddSingleton<IScheduledEventStorage, PostgresScheduledEventStorage>();

        services.AddSingleton<IPostgresNotificationListener, PostgresNotificationListener>();

        services.AddSingleton<ISubscriptionStorage, PostgresSubscriptionStorage>();

        if (options.Postgres.EnableNotifications)
        {
            services.AddHostedService<PostgresListenerService>();
        }
    }
}
