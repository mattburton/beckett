namespace Beckett.Subscriptions.Retries.Events;

public record RetryFailed(
    Guid Id,
    DateTimeOffset Timestamp
);
