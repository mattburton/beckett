using Beckett.Database.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Database;

public static class ServiceCollectionExtensions
{
    public static void AddDatabaseSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton<IDataSource, DataSource>();

        services.AddSingleton<INotificationListener, NotificationListener>();

        if (options.Database.RunMigrationsAtStartup)
        {
            services.AddHostedService<MigrationService>();
        }
    }
}
