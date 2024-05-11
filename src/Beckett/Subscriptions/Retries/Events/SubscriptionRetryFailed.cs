using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record SubscriptionRetryFailed(
    string SubscriptionName,
    string Topic,
    string StreamId,
    long StreamPosition,
    int Attempts,
    ExceptionData Exception,
    DateTimeOffset Timestamp
);
