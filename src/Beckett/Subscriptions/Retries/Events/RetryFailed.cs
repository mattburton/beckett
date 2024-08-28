namespace Beckett.Subscriptions.Retries.Events;

public record RetryFailed(
    Guid Id,
    int Attempt,
    ExceptionData? Error,
    DateTimeOffset Timestamp
);
