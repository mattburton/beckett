using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder UseSubscriptionRetries(this IBeckettBuilder builder)
    {
        builder.Map<SubscriptionRetryError>("$subscription_retry_error");
        builder.Map<SubscriptionRetryFailed>("$subscription_retry_failed");
        builder.Map<SubscriptionRetrySucceeded>("$subscription_retry_succeeded");
        builder.Map<SubscriptionError>("$subscription_error");

        builder.Services.AddSingleton<IRetryManager, RetryManager>();

        builder.Services.AddScoped<SubscriptionErrorHandler>();
        builder.Services.AddScoped<SubscriptionRetryErrorHandler>();
        builder.Services.AddScoped<SubscriptionRetryFailedHandler>();

        builder.AddSubscription("$retry:subscription_error")
            .Topic(RetryConstants.Topic)
            .Message<SubscriptionError>()
            .Handler<SubscriptionErrorHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry:subscription_retry_error")
            .Topic(RetryConstants.Topic)
            .Message<SubscriptionRetryError>()
            .Handler<SubscriptionRetryErrorHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry:subscription_retry_failed")
            .Topic(RetryConstants.Topic)
            .Message<SubscriptionRetryFailed>()
            .Handler<SubscriptionRetryFailedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        return builder;
    }
}
