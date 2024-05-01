namespace Beckett.Subscriptions.Retries.Events;

public record RetryCreated(
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    DateTimeOffset Timestamp
);
