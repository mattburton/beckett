namespace Beckett.Subscriptions.Retries.Events;

public record SubscriptionRetrySucceeded(
    string SubscriptionName,
    string Topic,
    string StreamId,
    long StreamPosition,
    int Attempts,
    DateTimeOffset Timestamp
);
