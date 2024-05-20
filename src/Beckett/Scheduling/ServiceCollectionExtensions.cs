using Beckett.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Scheduling;

public static class ServiceCollectionExtensions
{
    public static void AddScheduledMessageSupport(this IServiceCollection services, SchedulingOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IMessageScheduler, MessageScheduler>();

        services.AddHostedService<BootstrapRecurringMessages>();

        services.AddHostedService<RecurringMessageService>();

        services.AddHostedService<ScheduledMessageService>();
    }
}
