using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder UseSubscriptionRetries(this IBeckettBuilder builder)
    {
        builder.MapMessage<SubscriptionRetryError>("$subscription_retry_error");
        builder.MapMessage<SubscriptionRetryFailed>("$subscription_retry_failed");
        builder.MapMessage<SubscriptionRetrySucceeded>("$subscription_retry_succeeded");
        builder.MapMessage<SubscriptionError>("$subscription_error");

        builder.Services.AddSingleton<IRetryManager, RetryManager>();

        builder.Services.AddScoped<SubscriptionErrorHandler>();
        builder.Services.AddScoped<SubscriptionRetryErrorHandler>();

        builder.AddSubscription<SubscriptionErrorHandler, SubscriptionError>(
            "$subscription_error",
            (handler, message, token) => handler.Handle(message, token),
            configuration => configuration.MaxRetryCount = 0
        );

        builder.AddSubscription<SubscriptionRetryErrorHandler, SubscriptionRetryError>(
            "$subscription_retry_error",
            (handler, message, token) => handler.Handle(message, token),
            configuration => configuration.MaxRetryCount = 0
        );

        return builder;
    }
}
