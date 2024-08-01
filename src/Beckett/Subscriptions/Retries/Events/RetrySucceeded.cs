namespace Beckett.Subscriptions.Retries.Events;

public record RetrySucceeded(
    long CheckpointId,
    string SubscriptionGroupName,
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    DateTimeOffset Timestamp
);
