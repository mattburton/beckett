using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions options)
    {
        options.Events.Map<RetryError>("$retry_error");
        options.Events.Map<RetryFailed>("$retry_failed");
        options.Events.Map<RetrySucceeded>("$retry_succeeded");
        options.Events.Map<SubscriptionError>("$subscription_error");

        services.AddSingleton<IRetryService, RetryService>();

        services.AddScoped<SubscriptionErrorHandler>();
        services.AddScoped<RetryErrorHandler>();

        options.Subscriptions.AddSubscription<SubscriptionErrorHandler, SubscriptionError>(
            "$retry_created_subscription",
            (handler, @event, token) => handler.Handle(@event, token)
        );

        options.Subscriptions.AddSubscription<RetryErrorHandler, RetryError>(
            "$retry_scheduled_subscription",
            (handler, @event, token) => handler.Handle(@event, token)
        );
    }
}
