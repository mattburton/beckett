namespace Beckett.Subscriptions.Retries.Events;

public record RetryAttemptFailed(
    Guid Id,
    ExceptionData Error,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
