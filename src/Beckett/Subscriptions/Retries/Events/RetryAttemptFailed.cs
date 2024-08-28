namespace Beckett.Subscriptions.Retries.Events;

public record RetryAttemptFailed(
    Guid Id,
    int Attempt,
    ExceptionData Error,
    DateTimeOffset RetryAt,
    DateTimeOffset Timestamp
);
