using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder RetryClientSupport(this IBeckettBuilder builder)
    {
        builder.Map<RetryStarted>("$retry_started");
        builder.Map<RetryAttempted>("$retry_attempted");
        builder.Map<RetrySucceeded>("$retry_succeeded");
        builder.Map<RetryFailed>("$retry_failed");
        builder.Map<DeleteRetryRequested>("$delete_retry_requested");
        builder.Map<RetryDeleted>("$retry_deleted");
        builder.Map<ManualRetryRequested>("$manual_retry_requested");
        builder.Map<ManualRetryFailed>("$manual_retry_failed");

        return builder;
    }

    public static IBeckettBuilder RetryServerSupport(this IBeckettBuilder builder, RetryOptions options)
    {
        if (!options.Enabled)
        {
            return builder;
        }

        builder.Services.AddSingleton(options);

        builder.Services.AddSingleton<IRetryMonitor, RetryMonitor>();
        builder.Services.AddSingleton<IRetryManager, RetryManager>();

        builder.Services.AddSingleton<RetryStartedHandler>();
        builder.Services.AddSingleton<RetryAttemptedHandler>();
        builder.Services.AddSingleton<RetryFailedHandler>();
        builder.Services.AddSingleton<ManualRetryRequestedHandler>();
        builder.Services.AddSingleton<DeleteRetryRequestedHandler>();

        builder.Services.AddHostedService<RetryPollingService>();

        builder.Map<RetryStarted>("$retry_started");
        builder.Map<RetryAttempted>("$retry_attempted");
        builder.Map<RetrySucceeded>("$retry_succeeded");
        builder.Map<RetryFailed>("$retry_failed");
        builder.Map<DeleteRetryRequested>("$delete_retry_requested");
        builder.Map<RetryDeleted>("$retry_deleted");
        builder.Map<ManualRetryRequested>("$manual_retry_requested");
        builder.Map<ManualRetryFailed>("$manual_retry_failed");

        const string category = "$retry";

        builder.AddSubscription("$retry_started")
            .Category(category)
            .Message<RetryStarted>()
            .Handler<RetryStartedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_attempted")
            .Category(category)
            .Message<RetryAttempted>()
            .Handler<RetryAttemptedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_failed")
            .Category(category)
            .Message<RetryFailed>()
            .Handler<RetryFailedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$manual_retry_requested")
            .Category(category)
            .Message<ManualRetryRequested>()
            .Handler<ManualRetryRequestedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$delete_retry_requested")
            .Category(category)
            .Message<DeleteRetryRequested>()
            .Handler<DeleteRetryRequestedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        return builder;
    }
}
