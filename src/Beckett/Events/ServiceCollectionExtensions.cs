using Beckett.Events.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

public static class ServiceCollectionExtensions
{
    public static void AddEventSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton<IEventTypeMap>(options.Events.TypeMap);

        services.AddSingleton<IEventSerializer, EventSerializer>();

        services.AddHostedService<ScheduledEventService>();
    }
}
