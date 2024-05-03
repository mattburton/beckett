using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions options)
    {
        options.Events.Map<RetryCreated>("$retry_created");
        options.Events.Map<RetryFailed>("$retry_failed");
        options.Events.Map<RetryScheduled>("$retry_scheduled");
        options.Events.Map<RetrySucceeded>("$retry_succeeded");
        options.Events.Map<RetryUnsuccessful>("$retry_unsuccessful");

        services.AddSingleton<IRetryService, RetryService>();
        
        services.AddScoped<RetryCreatedHandler>();
        services.AddScoped<RetryScheduledHandler>();

        options.Subscriptions.AddSubscription<RetryCreatedHandler, RetryCreated>(
            "$retry_created_subscription",
            (handler, @event, token) => handler.Handle(@event, token)
        );

        options.Subscriptions.AddSubscription<RetryScheduledHandler, RetryScheduled>(
            "$retry_scheduled_subscription",
            (handler, @event, token) => handler.Handle(@event, token)
        );
    }
}
