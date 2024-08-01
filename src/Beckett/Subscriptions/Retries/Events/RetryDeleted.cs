namespace Beckett.Subscriptions.Retries.Events;

public record RetryDeleted(long CheckpointId, DateTimeOffset Timestamp);
