namespace Beckett.Subscriptions.Retries.Events;

public record RetrySucceeded(
    Guid Id,
    DateTimeOffset Timestamp
);
