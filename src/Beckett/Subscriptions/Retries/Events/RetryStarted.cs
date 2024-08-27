namespace Beckett.Subscriptions.Retries.Events;

public record RetryStarted(
    Guid Id,
    string GroupName,
    string Name,
    string StreamName,
    long StreamPosition,
    ExceptionData Error,
    int MaxRetryCount,
    DateTimeOffset? RetryAt,
    DateTimeOffset Timestamp
);
