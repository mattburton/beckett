using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder UseSubscriptionRetries(this IBeckettBuilder builder)
    {
        builder.MapEvent<RetryError>("$retry_error");
        builder.MapEvent<RetryFailed>("$retry_failed");
        builder.MapEvent<RetrySucceeded>("$retry_succeeded");
        builder.MapEvent<SubscriptionError>("$subscription_error");

        builder.Services.AddSingleton<IRetryService, RetryService>();

        builder.Services.AddScoped<SubscriptionErrorHandler>();
        builder.Services.AddScoped<RetryErrorHandler>();

        builder.AddSubscription<SubscriptionErrorHandler, SubscriptionError>(
            "$subscription_error",
            (handler, @event, token) => handler.Handle(@event, token)
        );

        builder.AddSubscription<RetryErrorHandler, RetryError>(
            "$retry_error",
            (handler, @event, token) => handler.Handle(@event, token)
        );

        return builder;
    }
}
