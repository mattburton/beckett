using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record SubscriptionError(
    string SubscriptionName,
    string Topic,
    string StreamId,
    long StreamPosition,
    ExceptionData Exception,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
