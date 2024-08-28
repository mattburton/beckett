namespace Beckett.Subscriptions.Retries.Events;

public record RetryScheduled(
    Guid Id,
    int Attempt,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
