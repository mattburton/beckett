using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder UseSubscriptionRetries(this IBeckettBuilder builder)
    {
        builder.Map<SubscriptionError>("$subscription_error");
        builder.Map<SubscriptionRetryError>("$subscription_retry_error");
        builder.Map<SubscriptionRetryFailed>("$subscription_retry_failed");
        builder.Map<SubscriptionRetrySucceeded>("$subscription_retry_succeeded");

        builder.Services.AddSingleton<IRetryManager, RetryManager>();

        builder.Services.AddScoped<SubscriptionErrorHandler>();
        builder.Services.AddScoped<SubscriptionRetryErrorHandler>();
        builder.Services.AddScoped<SubscriptionRetryFailedHandler>();

        const string category = "$retry";

        builder.AddSubscription("$retry:subscription_error")
            .Category(category)
            .Message<SubscriptionError>()
            .Handler<SubscriptionErrorHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry:subscription_retry_error")
            .Category(category)
            .Message<SubscriptionRetryError>()
            .Handler<SubscriptionRetryErrorHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry:subscription_retry_failed")
            .Category(category)
            .Message<SubscriptionRetryFailed>()
            .Handler<SubscriptionRetryFailedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        return builder;
    }
}
