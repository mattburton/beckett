using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder SubscriptionRetryModule(this IBeckettBuilder builder)
    {
        builder.Map<RetryStarted>("$retry_started");
        builder.Map<RetryError>("$retry_error");
        builder.Map<RetryFailed>("$retry_failed");
        builder.Map<RetrySucceeded>("$retry_succeeded");

        builder.Services.AddSingleton<IRetryManager, RetryManager>();
        builder.Services.AddSingleton<IRetryMonitor, RetryMonitor>();
        builder.Services.AddHostedService<RetryPollingService>();

        builder.Services.AddScoped<RetryStartedHandler>();
        builder.Services.AddScoped<RetryErrorHandler>();
        builder.Services.AddScoped<RetryFailedHandler>();

        const string category = "$retry";

        builder.AddSubscription("$retry_started")
            .Category(category)
            .Message<RetryStarted>()
            .Handler<RetryStartedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_error")
            .Category(category)
            .Message<RetryError>()
            .Handler<RetryErrorHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_failed")
            .Category(category)
            .Message<RetryFailed>()
            .Handler<RetryFailedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        return builder;
    }
}
