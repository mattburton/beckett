using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record SubscriptionRetryError(
    string SubscriptionName,
    string Topic,
    string StreamId,
    long StreamPosition,
    int Attempts,
    ExceptionData Exception,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
