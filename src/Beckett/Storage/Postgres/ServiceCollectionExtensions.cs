using Beckett.Events;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Storage.Postgres;

internal static class ServiceCollectionExtensions
{
    public static void AddPostgresSupport(this IServiceCollection services, BeckettOptions options)
    {
        if (!options.Postgres.Enabled)
        {
            return;
        }

        services.AddSingleton<IPostgresDatabase, PostgresDatabase>();

        services.AddSingleton<IEventStorage, PostgresEventStorage>();

        services.AddSingleton<IPostgresNotificationListener, PostgresNotificationListener>();

        services.AddSingleton<ISubscriptionStorage, PostgresSubscriptionStorage>();
    }
}
