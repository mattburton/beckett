namespace Beckett.Subscriptions.Retries.Events;

public record RetrySucceeded(
    Guid Id,
    string ApplicationName,
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    DateTimeOffset Timestamp
);
