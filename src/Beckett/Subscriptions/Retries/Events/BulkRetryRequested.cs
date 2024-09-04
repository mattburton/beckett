namespace Beckett.Subscriptions.Retries.Events;

public record BulkRetryRequested(List<Guid> RetryIds, DateTimeOffset Timestamp);
