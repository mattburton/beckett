namespace Beckett.Subscriptions.Retries.Events;

public record DeleteRetryRequested(
    Guid Id,
    DateTimeOffset Timestamp
);
