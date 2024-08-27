namespace Beckett.Subscriptions.Retries.Events;

public record ManualRetryFailed(
    Guid Id,
    ExceptionData? Error,
    DateTimeOffset Timestamp
);
