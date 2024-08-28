namespace Beckett.Subscriptions.Retries.Events;

public record ManualRetryFailed(
    Guid Id,
    int Attempt,
    ExceptionData? Error,
    DateTimeOffset Timestamp
);
