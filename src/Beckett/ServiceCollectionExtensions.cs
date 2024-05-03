using Beckett.Events;
using Beckett.Storage.Postgres;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBeckett(this IServiceCollection services, Action<BeckettOptions> configure)
    {
        var beckett = new BeckettOptions();

        configure(beckett);

        RunConfigurators(services, beckett);

        services.AddSingleton(beckett);

        services.AddPostgresSupport(beckett);

        services.AddEventSupport(beckett);

        services.AddSubscriptionSupport(beckett);

        services.AddSingleton<IEventStore, EventStore>();

        services.AddHostedService<BackgroundService>();

        return services;
    }

    private static void RunConfigurators(IServiceCollection services, BeckettOptions beckett)
    {
        var configuratorType = typeof(IConfigureBeckett);

        var configurators = beckett.GetAssemblyTypes()
            .Where(x => x.GetInterfaces().Any(i => i == configuratorType))
            .Select(Activator.CreateInstance)
            .Cast<IConfigureBeckett>();

        foreach (var configurator in configurators)
        {
            configurator.Configure(services, beckett);
        }
    }
}
