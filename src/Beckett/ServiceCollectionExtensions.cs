using Beckett.Database;
using Beckett.Events;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett;

public static class ServiceCollectionExtensions
{
    public static void AddBeckett(this IServiceCollection services, Action<BeckettOptions> configure)
    {
        var options = new BeckettOptions();

        configure(options);

        RunConfigurators(services, options);

        services.AddSingleton(options);

        services.AddDatabaseSupport(options);

        services.AddEventSupport(options);

        services.AddSubscriptionSupport(options);

        services.AddTransient<IEventStore, EventStore>();
    }

    private static void RunConfigurators(IServiceCollection services, BeckettOptions options)
    {
        var configuratorType = typeof(IConfigureBeckett);

        var configurators = options.GetAssemblyTypes()
            .Where(x => x.GetInterfaces().Any(i => i == configuratorType))
            .Select(Activator.CreateInstance)
            .Cast<IConfigureBeckett>();

        foreach (var configurator in configurators)
        {
            configurator.Configure(services, options);
        }
    }
}
