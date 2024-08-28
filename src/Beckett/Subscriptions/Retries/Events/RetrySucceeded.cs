namespace Beckett.Subscriptions.Retries.Events;

public record RetrySucceeded(
    Guid Id,
    int Attempt,
    DateTimeOffset Timestamp
);
