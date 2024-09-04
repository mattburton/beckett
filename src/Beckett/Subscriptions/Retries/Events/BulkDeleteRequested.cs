namespace Beckett.Subscriptions.Retries.Events;

public record BulkDeleteRequested(List<Guid> RetryIds, DateTimeOffset Timestamp);
