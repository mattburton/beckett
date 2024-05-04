using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder UseSubscriptionRetries(this IBeckettBuilder builder)
    {
        builder.MapEvent<SubscriptionRetryError>("$subscription_retry_error");
        builder.MapEvent<SubscriptionRetryFailed>("$subscription_retry_failed");
        builder.MapEvent<SubscriptionRetrySucceeded>("$subscription_retry_succeeded");
        builder.MapEvent<SubscriptionError>("$subscription_error");

        builder.Services.AddSingleton<IRetryManager, RetryManager>();

        builder.Services.AddScoped<SubscriptionErrorHandler>();
        builder.Services.AddScoped<SubscriptionRetryErrorHandler>();

        builder.AddSubscription<SubscriptionErrorHandler, SubscriptionError>(
            "$subscription_error",
            (handler, @event, token) => handler.Handle(@event, token),
            configuration => configuration.MaxRetryCount = 0
        );

        builder.AddSubscription<SubscriptionRetryErrorHandler, SubscriptionRetryError>(
            "$subscription_retry_error",
            (handler, @event, token) => handler.Handle(@event, token),
            configuration => configuration.MaxRetryCount = 0
        );

        return builder;
    }
}
