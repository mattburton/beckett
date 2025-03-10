using Beckett.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Scheduling;

public static class ServiceCollectionExtensions
{
    internal static void AddScheduledMessageSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Scheduling);

        services.AddSingleton<IMessageScheduler, MessageScheduler>();

        services.AddSingleton<ITransactionalMessageScheduler, MessageScheduler>();

        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddHostedService<ScheduledMessageService>();
    }
}
