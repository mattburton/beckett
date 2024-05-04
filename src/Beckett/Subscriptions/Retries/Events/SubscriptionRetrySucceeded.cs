namespace Beckett.Subscriptions.Retries.Events;

public record SubscriptionRetrySucceeded(
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    DateTimeOffset Timestamp
);
