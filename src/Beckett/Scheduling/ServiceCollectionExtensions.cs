using Beckett.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Scheduling;

public static class ServiceCollectionExtensions
{
    public static void AddScheduledMessageSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Scheduling);

        services.AddSingleton<IMessageScheduler, MessageScheduler>();

        services.AddSingleton<ITransactionalMessageScheduler, MessageScheduler>();

        services.AddSingleton<IRecurringMessageManager, RecurringMessageManager>();

        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        services.AddHostedService<BootstrapRecurringMessages>();

        services.AddHostedService<RecurringMessageService>();

        services.AddHostedService<ScheduledMessageService>();
    }
}
