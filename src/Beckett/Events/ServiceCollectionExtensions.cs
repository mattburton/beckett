using Beckett.Events.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

public static class ServiceCollectionExtensions
{
    public static void AddEventSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Events.TypeMap);

        services.AddSingleton<EventSerializer>();

        services.AddHostedService<ScheduledEventService>();
    }
}
