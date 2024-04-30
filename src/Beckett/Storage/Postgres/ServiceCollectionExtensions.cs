using Beckett.Storage.Postgres.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Storage.Postgres;

public static class ServiceCollectionExtensions
{
    public static void AddPostgresSupport(this IServiceCollection services, BeckettOptions options)
    {
        if (!options.Postgres.Enabled)
        {
            return;
        }

        services.AddSingleton<IPostgresDatabase, PostgresDatabase>();

        services.AddSingleton<IPostgresNotificationListener, PostgresNotificationListener>();

        services.AddSingleton<IStorageProvider, PostgresStorageProvider>();

        if (options.Postgres.RunMigrationsAtStartup)
        {
            services.AddHostedService<PostgresMigrationService>();
        }
    }
}
