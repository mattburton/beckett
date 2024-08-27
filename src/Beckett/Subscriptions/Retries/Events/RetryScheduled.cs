namespace Beckett.Subscriptions.Retries.Events;

public record RetryScheduled(
    Guid Id,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
