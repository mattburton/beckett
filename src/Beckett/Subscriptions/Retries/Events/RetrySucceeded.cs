namespace Beckett.Subscriptions.Retries.Events;

public record RetrySucceeded(
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    DateTimeOffset Timestamp
);
