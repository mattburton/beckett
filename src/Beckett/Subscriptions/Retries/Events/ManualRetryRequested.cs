namespace Beckett.Subscriptions.Retries.Events;

public record ManualRetryRequested(
    Guid Id,
    DateTimeOffset Timestamp
);
