namespace Beckett.Subscriptions.Retries.Events;

public record RetryDeleted(
    Guid Id,
    DateTimeOffset Timestamp
);
