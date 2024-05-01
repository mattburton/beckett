namespace Beckett.Subscriptions.Retries.Events;

public record RetryScheduled(
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
