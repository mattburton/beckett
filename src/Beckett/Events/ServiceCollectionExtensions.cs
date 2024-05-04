using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

public static class ServiceCollectionExtensions
{
    public static void AddEventSupport(this IServiceCollection services, EventOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IEventSerializer, EventSerializer>();
    }
}
