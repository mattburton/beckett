using Beckett.Messages.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Messages.Scheduling;

public static class ServiceCollectionExtensions
{
    public static void AddScheduledMessageSupport(this IServiceCollection services, ScheduledMessageOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IMessageScheduler, MessageScheduler>();

        services.AddHostedService<ScheduledMessageService>();
    }
}
