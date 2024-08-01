namespace Beckett.Subscriptions.Retries.Events;

public record DeleteRetryRequested(
    long CheckpointId,
    DateTimeOffset Timestamp
);
