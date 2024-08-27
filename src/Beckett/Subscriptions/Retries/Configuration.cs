using Beckett.Subscriptions.Retries.EventHandlers;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class Configuration
{
    public static IBeckettBuilder RetrySupport(this IBeckettBuilder builder, BeckettOptions options)
    {
        MapMessageTypes(builder);

        if (!options.Subscriptions.Enabled || !options.Subscriptions.Retries.Enabled)
        {
            return builder;
        }

        RegisterHandlers(builder);

        ConfigureSubscriptions(builder);

        return builder;
    }

    private static void ConfigureSubscriptions(IBeckettBuilder builder)
    {
        const string category = "$retry";

        builder.AddSubscription("$delete_retry_requested")
            .Category(category)
            .Message<DeleteRetryRequested>()
            .Handler<DeleteRetryRequestedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$manual_retry_requested")
            .Category(category)
            .Message<ManualRetryRequested>()
            .Handler<ManualRetryRequestedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_attempt_failed")
            .Category(category)
            .Message<RetryAttemptFailed>()
            .Handler<RetryAttemptFailedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_scheduled")
            .Category(category)
            .Message<RetryScheduled>()
            .Handler<RetryScheduledHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);

        builder.AddSubscription("$retry_started")
            .Category(category)
            .Message<RetryStarted>()
            .Handler<RetryStartedHandler>((handler, message, token) => handler.Handle(message, token))
            .MaxRetryCount(0);
    }

    private static void RegisterHandlers(IBeckettBuilder builder)
    {
        builder.Services.AddSingleton<DeleteRetryRequestedHandler>();
        builder.Services.AddSingleton<ManualRetryRequestedHandler>();
        builder.Services.AddSingleton<RetryAttemptFailedHandler>();
        builder.Services.AddSingleton<RetryScheduledHandler>();
        builder.Services.AddSingleton<RetryStartedHandler>();
    }

    private static void MapMessageTypes(IBeckettBuilder builder)
    {
        builder.Map<DeleteRetryRequested>("$delete_retry_requested");
        builder.Map<ManualRetryFailed>("$manual_retry_failed");
        builder.Map<ManualRetryRequested>("$manual_retry_requested");
        builder.Map<RetryAttemptFailed>("$retry_attempt_failed");
        builder.Map<RetryDeleted>("$retry_deleted");
        builder.Map<RetryFailed>("$retry_failed");
        builder.Map<RetryScheduled>("$retry_scheduled");
        builder.Map<RetryStarted>("$retry_started");
        builder.Map<RetrySucceeded>("$retry_succeeded");
    }
}
