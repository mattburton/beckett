using System.Reflection;
using Beckett.Events;
using Beckett.Storage.Postgres;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBeckett(this IServiceCollection services, Action<BeckettOptions> configure)
    {
        var options = new BeckettOptions();

        configure(options);

        RunConfigurators(services, options);

        services.AddSingleton(options);

        services.AddPostgresSupport(options);

        services.AddEventSupport(options);

        services.AddSubscriptionSupport(options);

        services.AddSingleton<IEventStore, EventStore>();

        services.AddHostedService<ServiceHost>();

        return services;
    }

    private static void RunConfigurators(IServiceCollection services, BeckettOptions options)
    {
        var configuratorType = typeof(IConfigureBeckett);

        var configurators = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x =>
            {
                try
                {
                    return x.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(x => x.GetInterfaces().Any(i => i == configuratorType))
            .Select(Activator.CreateInstance)
            .Cast<IConfigureBeckett>();

        foreach (var configurator in configurators)
        {
            configurator.Configure(services, options);
        }
    }
}
