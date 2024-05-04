using Beckett.ScheduledEvents.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.ScheduledEvents;

public static class ServiceCollectionExtensions
{
    public static void AddScheduledEventSupport(this IServiceCollection services, ScheduledEventOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IEventScheduler, EventScheduler>();

        services.AddHostedService<ScheduledEventService>();
    }
}
