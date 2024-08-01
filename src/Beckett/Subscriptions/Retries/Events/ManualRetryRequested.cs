namespace Beckett.Subscriptions.Retries.Events;

public record ManualRetryRequested(
    long CheckpointId,
    DateTimeOffset Timestamp
);
