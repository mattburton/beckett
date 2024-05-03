using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions beckett)
    {
        beckett.Events.Map<RetryCreated>("$retry_created");
        beckett.Events.Map<RetryFailed>("$retry_failed");
        beckett.Events.Map<RetryScheduled>("$retry_scheduled");
        beckett.Events.Map<RetrySucceeded>("$retry_succeeded");
        beckett.Events.Map<RetryUnsuccessful>("$retry_unsuccessful");

        services.AddScoped<RetryCreatedHandler>();
        services.AddScoped<RetryScheduledHandler>();

        beckett.Subscriptions.AddSubscription<RetryCreatedHandler, RetryCreated>(
            "$retry_created_subscription",
            (handler, @event, token) => handler.Handle(@event, token)
        );

        beckett.Subscriptions.AddSubscription<RetryScheduledHandler, RetryScheduled>(
            "$retry_scheduled_subscription",
            (handler, @event, token) => handler.Handle(@event, token)
        );
    }
}
