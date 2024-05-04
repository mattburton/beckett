using Beckett.Events.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

public static class ServiceCollectionExtensions
{
    public static void AddEventSupport(this IServiceCollection services)
    {
        services.AddSingleton<IEventSerializer, EventSerializer>();

        services.AddHostedService<ScheduledEventService>();
    }
}
